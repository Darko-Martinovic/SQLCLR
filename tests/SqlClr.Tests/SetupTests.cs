namespace SqlClr.Tests;

/// <summary>
/// The setup scripts and CleanUp.sql on a SQL Server for Linux of their own, since they change
/// what the server trusts. One test walks through installing, reinstalling and removing in order.
/// </summary>
public sealed class SetupTests : IAsyncLifetime
{
    private readonly SqlServerContainer _server = new();

    public async ValueTask InitializeAsync()
    {
        await _server.StartAsync(enableClr: false);
        await _server.RunAsync("master", "CREATE DATABASE One; CREATE DATABASE Two;");
    }

    public ValueTask DisposeAsync() => _server.DisposeAsync();

    [Fact]
    public async Task Setup_and_clean_up_leave_the_server_as_they_found_it()
    {
        // With CLR disabled, SetupHtml installs and says what to do
        var setup = await _server.RunScriptAsync("One", Repository.SetupHtml);
        setup.AssertNoErrors();
        Assert.Contains(setup.Messages, m => m.StartsWith("CLR is disabled on this server."));

        await _server.EnableClrAsync();
        Assert.Contains("<table", await Html("One"));

        // Again, and in a second database: still one trusted assembly
        (await _server.RunScriptAsync("One", Repository.SetupHtml)).AssertNoErrors();
        (await _server.RunScriptAsync("Two", Repository.SetupHtml)).AssertNoErrors();
        Assert.Equal(1, await TrustedAssemblies());

        // Cleaning up one database keeps the trust the other needs
        (await _server.RunScriptAsync("Two", Repository.CleanUp)).AssertNoErrors();
        Assert.Equal(0, await Count("Two", "SELECT COUNT(*) FROM sys.assemblies WHERE is_user_defined = 1"));
        Assert.Equal(0, await Count("Two", "SELECT COUNT(*) FROM sys.schemas WHERE name = N'EMAIL'"));
        Assert.Equal(1, await TrustedAssemblies());
        Assert.Contains("<table", await Html("One"));

        // The last clean up removes the trust
        (await _server.RunScriptAsync("One", Repository.CleanUp)).AssertNoErrors();
        Assert.Equal(0, await Count("One", "SELECT COUNT(*) FROM sys.schemas WHERE name = N'EMAIL'"));
        Assert.Equal(0, await TrustedAssemblies());
    }

    [Fact]
    public async Task SetupSendMail_refuses_Linux_and_installs_nothing()
    {
        await _server.EnableClrAsync();

        var setup = await _server.RunScriptAsync("Two", Repository.SetupSendMail);

        var error = Assert.Single(setup.Errors);
        Assert.StartsWith("SQL Server on Linux runs SAFE assemblies only", error.Message);
        Assert.Equal(0, await Count("Two", "SELECT COUNT(*) FROM sys.objects WHERE name IN (N'CLRSendMail', N'Profiles')"));
    }

    private async Task<string> Html(string database) =>
        (await _server.RunAsync(database, "SELECT EMAIL.QueryToHtml(N'SELECT 1 AS a', NULL, NULL, NULL, 0, 0, NULL);")).Value!;

    private Task<int> TrustedAssemblies() =>
        Count("master", "SELECT COUNT(*) FROM sys.trusted_assemblies WHERE description LIKE N'simpletalk.sqlclr.%'");

    private async Task<int> Count(string database, string sql) =>
        int.Parse((await _server.RunAsync(database, sql)).Value!);
}
