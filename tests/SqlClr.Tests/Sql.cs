using System.Reflection;
using System.Text.RegularExpressions;

using Microsoft.Data.SqlClient;

using Testcontainers.MsSql;

namespace SqlClr.Tests;

/// <summary>What a batch returned: its results, its messages and its errors.</summary>
public sealed record Result(IReadOnlyList<Table> Tables, IReadOnlyList<string> Messages, IReadOnlyList<SqlError> Errors)
{
    public Table Table => Assert.Single(Tables);

    /// <summary>The single value of a single result.</summary>
    public string? Value => Assert.Single(Table.Rows)[0];

    public void AssertNoErrors() => Assert.True(Errors.Count == 0, string.Join("\n", Errors.Select(e => e.Message)));
}

public sealed record Table(IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyList<string?>> Rows);

/// <summary>
/// Runs batches as a client does. Errors don't throw: they are collected, with the messages.
/// </summary>
public static partial class Sql
{
    public static Task<Result> RunAsync(string connectionString, string sql) => RunBatchesAsync(connectionString, [sql]);

    /// <summary>
    /// Runs a script of batches separated by GO on one connection, as SSMS and sqlcmd do, so
    /// SET NOEXEC ON in one batch holds for the next ones.
    /// </summary>
    public static Task<Result> RunScriptAsync(string connectionString, string script) =>
        RunBatchesAsync(connectionString, Go().Split(script).Where(b => b.Trim().Length > 0));

    private static async Task<Result> RunBatchesAsync(string connectionString, IEnumerable<string> batches)
    {
        var messages = new List<string>();
        var errors = new List<SqlError>();
        var tables = new List<Table>();

        await using var connection = new SqlConnection(connectionString) { FireInfoMessageEventOnUserErrors = true };
        connection.InfoMessage += (_, e) =>
        {
            foreach (SqlError error in e.Errors)
            {
                if (error.Class > 10)
                    errors.Add(error);
                else
                    messages.Add(error.Message);
            }
        };
        await connection.OpenAsync();

        foreach (var batch in batches)
        {
            await using var command = new SqlCommand(batch, connection) { CommandTimeout = 120 };
            await using var reader = await command.ExecuteReaderAsync();
            do
            {
                if (reader.FieldCount == 0)
                    continue;

                var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
                var rows = new List<IReadOnlyList<string?>>();
                while (await reader.ReadAsync())
                    rows.Add(Enumerable.Range(0, reader.FieldCount).Select(i => reader.IsDBNull(i) ? null : Convert.ToString(reader.GetValue(i))).ToList());
                tables.Add(new Table(columns, rows));
            } while (await reader.NextResultAsync());
        }

        return new Result(tables, messages, errors);
    }

    public static string WithDatabase(string connectionString, string database) =>
        new SqlConnectionStringBuilder(connectionString) { InitialCatalog = database }.ToString();

    /// <summary>A string as an N'...' literal.</summary>
    public static string Literal(string? value) => value == null ? "NULL" : "N'" + value.Replace("'", "''") + "'";

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex Go();
}

/// <summary>
/// SQL Server for Linux in a container: 2025 unless MSSQL_IMAGE names another image, as CI
/// does for 2022.
/// </summary>
public sealed class SqlServerContainer : IAsyncDisposable
{
    private static readonly string Image =
        Environment.GetEnvironmentVariable("MSSQL_IMAGE") ?? "mcr.microsoft.com/mssql/server:2025-latest";

    private readonly MsSqlContainer _container = new MsSqlBuilder(Image).Build();

    public async Task StartAsync(bool enableClr = true)
    {
        try
        {
            await _container.StartAsync();
        }
        catch (Exception e)
        {
            // Show why SQL Server didn't start, not only that it didn't
            var (stdout, stderr) = await _container.GetLogsAsync();
            throw new InvalidOperationException($"SQL Server didn't start:\n{stdout}\n{stderr}", e);
        }

        if (enableClr)
            await EnableClrAsync();
    }

    public Task EnableClrAsync() =>
        RunAsync("master", "EXEC sp_configure 'show advanced options', 1; RECONFIGURE; EXEC sp_configure 'clr enabled', 1; RECONFIGURE;");

    public string ConnectionString(string database) => Sql.WithDatabase(_container.GetConnectionString(), database);

    public Task<Result> RunAsync(string database, string sql) => Sql.RunAsync(ConnectionString(database), sql);

    public Task<Result> RunScriptAsync(string database, string script) => Sql.RunScriptAsync(ConnectionString(database), script);

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}

public static class Repository
{
    public static string Root { get; } = typeof(Repository).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
        .Single(a => a.Key == "RepositoryRoot").Value!;

    public static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    public static string SetupHtml => Read("Setup/SetupHtml.sql");

    public static string SetupSendMail => Read("Setup/SetupSendMail.sql");

    public static string CleanUp => Read("Setup/CleanUp.sql");
}
