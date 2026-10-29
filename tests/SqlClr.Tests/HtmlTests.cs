using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SqlClr.Tests;

/// <summary>
/// Starts SQL Server for Linux and installs the committed SetupHtml.sql in a database, once for
/// all the tests: the HTML functions are SAFE, so they run there.
/// </summary>
public sealed class HtmlFixture : IAsyncLifetime
{
    private const string Database = "HtmlTest";

    public SqlServerContainer Server { get; } = new();

    public async ValueTask InitializeAsync()
    {
        await Server.StartAsync();
        await Server.RunAsync("master", $"CREATE DATABASE {Database};");
        (await Server.RunScriptAsync(Database, Repository.SetupHtml)).AssertNoErrors();
    }

    public Task<Result> RunAsync(string sql) => Server.RunAsync(Database, sql);

    /// <summary>The HTML of QueryToHtml, parsed.</summary>
    public async Task<XElement> HtmlAsync(string query, string? parameters = null, string? caption = null, string? footer = null,
        int rm = 0, int rco = 0, string? style = null)
    {
        var result = await RunAsync(
            $"SELECT EMAIL.QueryToHtml({Sql.Literal(query)}, {Sql.Literal(parameters)}, {Sql.Literal(caption)}, {Sql.Literal(footer)}, {rm}, {rco}, {Sql.Literal(style)});");
        result.AssertNoErrors();
        return XElement.Parse(result.Value!);
    }

    public ValueTask DisposeAsync() => Server.DisposeAsync();
}

public sealed class HtmlTests(HtmlFixture html) : IClassFixture<HtmlFixture>
{
    private const string Databases = "SELECT name, database_id FROM sys.databases WHERE database_id <= 3 ORDER BY database_id";

    [Fact]
    public async Task A_query_becomes_a_table()
    {
        var document = await html.HtmlAsync(Databases, caption: "Databases", footer: "#");

        var table = Assert.Single(Tables(document));
        Assert.Equal("Databases", table.Element("caption")!.Value);
        Assert.Equal(["name", "database_id"], Header(table));
        Assert.Equal([["master", "1"], ["tempdb", "2"], ["model", "3"]], Body(table));
        var footer = table.Element("tfoot")!.Descendants("td").Single();
        // Version 1 counted the header too
        Assert.Equal("Total records : 3", footer.Value);
        Assert.Equal("2", footer.Attribute("colspan")!.Value);
    }

    [Fact]
    public async Task A_document_has_a_character_set_and_no_footer_unless_asked()
    {
        var document = await html.HtmlAsync(Databases);

        Assert.Equal("utf-8", document.Element("head")!.Element("meta")!.Attribute("charset")!.Value);
        var table = Assert.Single(Tables(document));
        Assert.Null(table.Element("caption"));
        Assert.Null(table.Element("tfoot"));
    }

    [Fact]
    public async Task Every_result_is_a_table_and_semicolons_in_literals_stay()
    {
        // Version 1 split the query at every semicolon
        var document = await html.HtmlAsync("SELECT N'a;b' AS x; SELECT 2 AS y;");

        var tables = Tables(document).ToList();
        Assert.Equal(2, tables.Count);
        Assert.Equal([["a;b"]], Body(tables[0]));
        Assert.Equal(["y"], Header(tables[1]));
    }

    [Fact]
    public async Task A_function_can_run_no_stored_procedure_but_reads_the_callers_temporary_tables()
    {
        // QueryToHtml is a function, and SQL Server lets functions run only some system procedures
        var procedure = await html.RunAsync("SELECT EMAIL.QueryToHtml(N'EXEC sp_executesql N''SELECT 5 AS a''', NULL, NULL, NULL, 0, 0, NULL);");
        var viaTable = await html.RunAsync("""
            CREATE TABLE #result (a int);
            INSERT INTO #result EXEC sp_executesql N'SELECT 5 AS a';
            SELECT EMAIL.QueryToHtml(N'SELECT a FROM #result', NULL, NULL, NULL, 0, 0, NULL);
            """);

        Assert.Contains("The query failed: Only functions and some extended stored procedures can be executed from within a function.", Assert.Single(procedure.Errors).Message);
        viaTable.AssertNoErrors();
        Assert.Equal([["5"]], Body(Assert.Single(Tables(XElement.Parse(viaTable.Value!)))));
    }

    [Fact]
    public async Task A_system_procedure_SQL_Server_allows_becomes_a_table()
    {
        var document = await html.HtmlAsync("EXEC sp_who");

        Assert.Equal("spid", Header(Assert.Single(Tables(document)))[0]);
    }

    [Theory]
    [InlineData("@v bigint = 5000000000", "5000000000")]
    [InlineData("@v varchar(10) = 'abc'", "abc")]
    [InlineData("@v decimal(10,2) = 1.5", "1.50")]
    [InlineData("@v datetime = '2020-01-01 13:45:00'", "2020-01-01 13:45:00.000")]
    [InlineData("@v nvarchar(20) = N'a, b'", "a, b")]
    public async Task Params_take_any_type_SQL_Server_declares(string declaration, string expected)
    {
        var document = await html.HtmlAsync("SELECT @v AS v", declaration);

        Assert.Equal([[expected]], Body(Assert.Single(Tables(document))));
    }

    [Fact]
    public async Task Values_and_captions_are_escaped()
    {
        var result = await html.RunAsync("SELECT EMAIL.QueryToHtml(N'SELECT N''a & <b>'' AS [<x>]', NULL, N'<i>caption</i>', N'#', 0, 0, NULL);");
        result.AssertNoErrors();

        Assert.Contains("a &amp; &lt;b&gt;", result.Value);
        Assert.Contains("&lt;i&gt;caption&lt;/i&gt;", result.Value);
        var table = Assert.Single(Tables(XElement.Parse(result.Value!)));
        Assert.Equal(["<x>"], Header(table));
        Assert.Equal([["a & <b>"]], Body(table));
    }

    [Fact]
    public async Task Null_and_empty_values_are_empty_cells()
    {
        var document = await html.HtmlAsync("SELECT CAST(NULL AS int) AS a, N'' AS b");

        Assert.Equal([["", ""]], Body(Assert.Single(Tables(document))));
    }

    [Fact]
    public async Task Values_are_the_same_in_any_language()
    {
        var result = await html.RunAsync("""
            SET LANGUAGE German;
            SELECT EMAIL.QueryToHtml(N'SELECT CAST(1.5 AS decimal(3, 1)) AS d, CAST(''2020-01-01 10:00:00.1234567'' AS datetime2) AS t', NULL, NULL, NULL, 0, 0, NULL);
            """);
        result.AssertNoErrors();

        Assert.Equal([["1.5", "2020-01-01 10:00:00.1234567"]], Body(Assert.Single(Tables(XElement.Parse(result.Value!)))));
    }

    [Fact]
    public async Task Rotating_on_a_column_makes_its_values_the_header()
    {
        var document = await html.HtmlAsync(Databases, footer: "#", rm: 2, rco: 0);

        var table = Assert.Single(Tables(document));
        Assert.Equal(["name", "master", "tempdb", "model"], Header(table));
        Assert.Equal([["database_id", "1", "2", "3"]], Body(table));
        // The footer spans the rotated table, and counts the query's rows
        var footer = table.Element("tfoot")!.Descendants("td").Single();
        Assert.Equal("4", footer.Attribute("colspan")!.Value);
        Assert.Equal("Total records : 3", footer.Value);
    }

    [Fact]
    public async Task Rotating_with_rco_minus_one_gives_a_key_value_header()
    {
        var document = await html.HtmlAsync(Databases, rm: 2, rco: -1);

        var table = Assert.Single(Tables(document));
        Assert.Equal(["Key", "Value", "Value1", "Value2"], Header(table));
        Assert.Equal([["name", "master", "tempdb", "model"], ["database_id", "1", "2", "3"]], Body(table));
    }

    [Theory]
    [InlineData("SELECT 1 AS a, 2 AS b, 3 AS c", true)]
    [InlineData("SELECT name, database_id FROM sys.databases WHERE database_id <= 3", false)]
    public async Task Auto_rotation_rotates_results_with_fewer_rows_than_columns(string query, bool rotated)
    {
        var document = await html.HtmlAsync(query, rm: 1);

        var header = Header(Assert.Single(Tables(document)));
        Assert.Equal(rotated, header[0] == "a" && header.Count == 2);
    }

    [Fact]
    public async Task An_rco_beyond_the_columns_is_an_error()
    {
        var result = await html.RunAsync($"SELECT EMAIL.QueryToHtml(N'{Databases}', NULL, NULL, NULL, 2, 5, NULL);");

        Assert.Contains("@rco is 5, but the result's columns are numbered 0 to 1, or -1 for a Key, Value header.", Assert.Single(result.Errors).Message);
    }

    [Theory]
    [InlineData("ST_BLUE", "#006699")]
    [InlineData("ST_RED", "#991821")]
    [InlineData("ST_GREEN", "#36752D")]
    [InlineData("ST_ROSE", "#652299")]
    [InlineData("ST_BROWN", "#A65B1A")]
    [InlineData("ST_BLACK", "#8C8C8C")]
    [InlineData("ST_SIMPLE", "#DEDEDE")]
    [InlineData("st_red", "#991821")]
    public async Task Built_in_styles_are_written_into_the_elements(string style, string headerColour)
    {
        var document = await html.HtmlAsync(Databases, style: style);

        var div = Assert.Single(document.Element("body")!.Elements("div"));
        Assert.Equal("datagrid" + style.ToUpperInvariant(), div.Attribute("class")!.Value);
        Assert.All(div.Descendants("th"), th => Assert.Contains($"background-color: {headerColour}", th.Attribute("style")!.Value));
        Assert.All(div.Descendants("td"), td => Assert.NotNull(td.Attribute("style")));
        // Every second row is shaded
        var rows = div.Element("table")!.Element("tbody")!.Elements("tr").ToList();
        Assert.DoesNotContain("background-color", rows[0].Element("td")!.Attribute("style")!.Value);
        Assert.Contains("background-color", rows[1].Element("td")!.Attribute("style")!.Value);
    }

    [Fact]
    public async Task No_style_writes_no_style()
    {
        var document = await html.HtmlAsync(Databases, style: "ST_NOSTYLE");

        Assert.DoesNotContain(document.Descendants(), e => e.Attribute("style") != null);
    }

    [Fact]
    public async Task A_custom_style_goes_in_the_head_and_names_the_tables_class()
    {
        var document = await html.HtmlAsync(Databases, style: "<style type=\"text/css\">table.mine td { color: red; }</style>");

        Assert.Contains("table.mine td", document.Element("head")!.Element("style")!.Value);
        var table = Assert.Single(Tables(document));
        Assert.Equal("mine", table.Attribute("class")!.Value);
        Assert.Null(table.Attribute("style"));
    }

    [Fact]
    public async Task An_unknown_style_is_an_error()
    {
        var result = await html.RunAsync($"SELECT EMAIL.QueryToHtml(N'{Databases}', NULL, NULL, NULL, 0, 0, N'ST_PINK');");

        Assert.Contains("@style 'ST_PINK' is not a style: use ST_BLUE, ST_RED", Assert.Single(result.Errors).Message);
    }

    [Fact]
    public async Task A_failing_query_is_an_error()
    {
        var result = await html.RunAsync("SELECT EMAIL.QueryToHtml(N'SELECT * FROM dbo.NoSuchTable', NULL, NULL, NULL, 0, 0, NULL);");

        // Version 1 returned the error message as the HTML, which then went out by e-mail
        Assert.Contains("The query failed: Invalid object name 'dbo.NoSuchTable'.", Assert.Single(result.Errors).Message);
    }

    [Fact]
    public async Task An_error_of_a_function_reaches_TRY_CATCH()
    {
        var result = await html.RunAsync("""
            BEGIN TRY
                SELECT EMAIL.QueryToHtml(N'SELECT * FROM dbo.NoSuchTable', NULL, NULL, NULL, 0, 0, NULL);
            END TRY
            BEGIN CATCH
                SELECT ERROR_NUMBER();
            END CATCH;
            """);

        Assert.Equal("6522", result.Tables[^1].Rows[0][0]);
    }

    [Theory]
    [InlineData("NULL", "@query is required.")]
    [InlineData("N'DECLARE @x int = 1;'", "The query returned no result to show.")]
    public async Task A_query_with_nothing_to_show_is_an_error(string query, string message)
    {
        var result = await html.RunAsync($"SELECT EMAIL.QueryToHtml({query}, NULL, NULL, NULL, 0, 0, NULL);");

        Assert.Contains(message, Assert.Single(result.Errors).Message);
    }

    [Fact]
    public async Task An_unknown_rotate_mode_is_an_error()
    {
        var result = await html.RunAsync($"SELECT EMAIL.QueryToHtml(N'{Databases}', NULL, NULL, NULL, 3, 0, NULL);");

        Assert.Contains("@rm is 3", Assert.Single(result.Errors).Message);
    }

    [Fact]
    public async Task ConCatHtml_puts_the_second_documents_tables_after_the_firsts()
    {
        var result = await html.RunAsync("""
            DECLARE @first nvarchar(max) = EMAIL.QueryToHtml(N'SELECT 1 AS a', NULL, NULL, NULL, 0, 0, N'ST_RED');
            DECLARE @second nvarchar(max) = EMAIL.QueryToHtml(N'SELECT 2 AS b', NULL, NULL, NULL, 0, 0, N'<style type="text/css">table.mine { color: red; }</style>');
            DECLARE @both nvarchar(max) = EMAIL.ConCatHtml(@first, @second);
            SELECT EMAIL.ConCatHtml(@both, @second);
            """);
        result.AssertNoErrors();

        var document = XElement.Parse(result.Value!);
        Assert.Equal(["a", "b", "b"], Tables(document).Select(t => Header(t)[0]));
        // The custom style sheet is added once; version 1 dropped tables of custom styles
        Assert.Single(document.Element("head")!.Elements("style"));
    }

    [Fact]
    public async Task ConCatHtml_takes_null_for_either_document()
    {
        var result = await html.RunAsync("""
            DECLARE @first nvarchar(max) = EMAIL.QueryToHtml(N'SELECT 1 AS a', NULL, NULL, NULL, 0, 0, NULL);
            SELECT CASE WHEN EMAIL.ConCatHtml(NULL, @first) = @first AND EMAIL.ConCatHtml(@first, NULL) = @first THEN 1 ELSE 0 END;
            """);

        Assert.Equal("1", result.Value);
    }

    [Fact]
    public async Task ConCatHtml_refuses_other_html()
    {
        var result = await html.RunAsync("SELECT EMAIL.ConCatHtml(N'<html><body><p>unclosed</body></html>', N'<html><body/></html>');");

        Assert.Contains("@mHtml is not HTML from QueryToHtml or ConCatHtml", Assert.Single(result.Errors).Message);
    }

    [Theory]
    [InlineData("EMAIL.CLRSendMail", "@profileName")]
    [InlineData("[EMAIL].[QueryToHtml]", "@query")]
    [InlineData("ConCatHtml", "@mHtml")]
    [InlineData("EMAIL.SaveProfile", "@hostName")]
    public async Task Help_describes_the_parameters(string name, string parameter)
    {
        var result = await html.RunAsync($"SELECT * FROM EMAIL.CustomSendMailHelp(N'{name}');");

        Assert.Equal(["ParameterName", "ParameterDescription"], result.Table.Columns);
        Assert.Contains(result.Table.Rows, row => row[0] == parameter);
    }

    [Fact]
    public async Task Help_names_an_unknown_procedure()
    {
        var result = await html.RunAsync("SELECT * FROM EMAIL.CustomSendMailHelp(N'dbo.Other');");

        Assert.Equal(["Error", "The procedure or the function dbo.Other does not exist!"], Assert.Single(result.Table.Rows));
    }

    [Fact]
    public void The_setup_scripts_are_built_from_the_current_sources()
    {
        var current = SetupScript.SourceHash.Compute(Repository.Root);

        foreach (var script in new[] { Repository.SetupHtml, Repository.SetupSendMail })
        {
            var recorded = Regex.Match(script, "^-- Sources: ([0-9a-f]{64})$", RegexOptions.Multiline).Groups[1].Value;
            Assert.True(recorded == current, "The setup scripts are older than the sources: dotnet run -c Release --project tools/SetupScript");
        }
    }

    private static IEnumerable<XElement> Tables(XElement document) => document.Element("body")!.Descendants("table");

    private static List<string> Header(XElement table) =>
        table.Element("thead")!.Descendants("th").Select(th => th.Value).ToList();

    private static List<List<string>> Body(XElement table) =>
        table.Element("tbody")!.Elements("tr").Select(tr => tr.Elements("td").Select(td => td.Value).ToList()).ToList();
}
