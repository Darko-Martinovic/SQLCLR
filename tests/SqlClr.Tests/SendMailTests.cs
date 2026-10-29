using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace SqlClr.Tests;

/// <summary>
/// Installs both setup scripts in a new database of a Windows SQL Server, with profiles that
/// send to a stand-in SMTP server in the test process. CLRSendMail needs an UNSAFE assembly, which
/// SQL Server runs on Windows only, so these tests run only when SQLCLR_SENDMAIL_SERVER is a
/// connection string to one on this machine, such as a LocalDB instance of its own:
///   SqlLocalDB create SqlClrTests -s
///   SQLCLR_SENDMAIL_SERVER=Server=(localdb)\SqlClrTests;Integrated Security=true;TrustServerCertificate=true
/// The tests enable CLR there, and drop their database at the end.
/// </summary>
public sealed class SendMailFixture : IAsyncLifetime
{
    public static readonly string? Server = Environment.GetEnvironmentVariable("SQLCLR_SENDMAIL_SERVER");

    public const string Password = "p@ss;w0rd'";

    private readonly string _database = "SendMailTest_" + Guid.NewGuid().ToString("N")[..8];

    public SmtpSink Sink { get; } = new();

    /// <summary>The folder the configuration 'test' allows attachments from.</summary>
    public string Folder => Path.Combine(Root, "allowed");

    /// <summary>The tests' own folder, removed at the end.</summary>
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "SqlClrSendMailTests-" + Guid.NewGuid().ToString("N")[..8]);

    public string ConnectionString(string database) => Sql.WithDatabase(Server!, database);

    public async ValueTask InitializeAsync()
    {
        if (Server == null)
            return;

        Directory.CreateDirectory(Folder);
        await Sql.RunAsync(ConnectionString("master"),
            $"EXEC sp_configure 'show advanced options', 1; RECONFIGURE; EXEC sp_configure 'clr enabled', 1; RECONFIGURE; CREATE DATABASE {_database};");
        (await Sql.RunScriptAsync(ConnectionString(_database), Repository.SetupHtml)).AssertNoErrors();
        (await Sql.RunScriptAsync(ConnectionString(_database), Repository.SetupSendMail)).AssertNoErrors();

        (await RunAsync($"""
            EXEC EMAIL.SaveProfile @profileName = N'sink', @hostName = N'127.0.0.1', @port = {Sink.Port}, @enableSsl = 0,
                 @userName = N'mailer', @password = {Sql.Literal(Password)}, @defaultFrom = N'sql@example.com', @defaultDisplayName = N'SQL Server';
            EXEC EMAIL.SaveProfile @profileName = N'tls', @hostName = N'127.0.0.1', @port = {Sink.Port}, @enableSsl = 1, @defaultFrom = N'sql@example.com';
            EXEC EMAIL.SaveProfile @profileName = N'closed', @hostName = N'127.0.0.1', @port = {ClosedPort()}, @enableSsl = 0, @defaultFrom = N'sql@example.com', @timeout = 5000;
            INSERT INTO EMAIL.Configurations (Name, MaxFileSize, NoPiping, LoggingLevel, AttachmentFolders)
            VALUES ('test', 1000, 0, 2, {Sql.Literal(Folder)});
            """)).AssertNoErrors();
    }

    public Task<Result> RunAsync(string sql) => Sql.RunAsync(ConnectionString(_database), sql);

    /// <summary>Runs CLRSendMail with these arguments, for the stand-in server's next delivery.</summary>
    public Task<Result> SendAsync(string arguments)
    {
        Sink.Clear();
        return RunAsync($"EXEC EMAIL.CLRSendMail {arguments};");
    }

    public async ValueTask DisposeAsync()
    {
        if (Server != null)
        {
            await Sql.RunScriptAsync(ConnectionString(_database), Repository.CleanUp);
            await Sql.RunAsync(ConnectionString("master"), $"ALTER DATABASE {_database} SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE {_database};");
            Directory.Delete(Root, recursive: true);
        }

        await Sink.DisposeAsync();
    }

    private static int ClosedPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}

public sealed class SendMailTests(SendMailFixture mail) : IClassFixture<SendMailFixture>
{
    private const string Skip = "CLRSendMail needs SQL Server on Windows: set SQLCLR_SENDMAIL_SERVER to run these tests";

    [Fact]
    public async Task A_message_reaches_the_server()
    {
        Assert.SkipWhen(SendMailFixture.Server == null, Skip);

        var result = await mail.SendAsync("""
            @profileName = N'sink', @mailTo = N'a@example.com; b@example.com', @mailCc = N'c@example.com',
            @blindCopyRec = N'd@example.com', @replyAddress = N'reply@example.com', @mailSubject = N'Café report',
            @mailBody = N'<html><body><p>Report</p></body></html>', @configName = N'test'
            """);

        result.AssertNoErrors();
        Assert.Contains("Sent successfully!", result.Messages);
        var delivery = mail.Sink.Single();
        Assert.Equal("mailer", delivery.UserName);
        Assert.Equal(SendMailFixture.Password, delivery.Password);
        Assert.Equal("sql@example.com", delivery.MailFrom);
        Assert.Equal(["a@example.com", "b@example.com", "c@example.com", "d@example.com"], delivery.Recipients.Order());

        var message = delivery.Message;
        Assert.Equal("SQL Server", message.From.Mailboxes.Single().Name);
        Assert.Equal(["a@example.com", "b@example.com"], message.To.Mailboxes.Select(m => m.Address));
        Assert.Equal(["c@example.com"], message.Cc.Mailboxes.Select(m => m.Address));
        Assert.Empty(message.Bcc);
        // Version 1 ignored @replyAddress
        Assert.Equal(["reply@example.com"], message.ReplyTo.Mailboxes.Select(m => m.Address));
        Assert.Equal("Café report", message.Subject);
        Assert.Contains("<p>Report</p>", message.HtmlBody);

        var saved = await mail.RunAsync("SELECT TOP (1) recipients, copy_recipients, reply_to, body_format FROM EMAIL.MailItems ORDER BY mailitem_id DESC;");
        Assert.Equal(["a@example.com;b@example.com;", "c@example.com;", "reply@example.com;", "HTML"], Assert.Single(saved.Table.Rows));
        var logged = await mail.RunAsync("SELECT TOP (1) Type, Message FROM EMAIL.MonitorLog ORDER BY ID DESC;");
        Assert.Equal(["Information", "E-mail successfully sent!"], Assert.Single(logged.Table.Rows));
    }

    [Fact]
    public async Task A_body_from_QueryToHtml_is_sent_as_html()
    {
        Assert.SkipWhen(SendMailFixture.Server == null, Skip);

        var result = await mail.RunAsync("""
            DECLARE @body nvarchar(max) = EMAIL.QueryToHtml(N'SELECT name FROM sys.databases WHERE database_id = 1', NULL, N'Databases', N'#', 0, 0, N'ST_GREEN');
            EXEC EMAIL.CLRSendMail @profileName = N'sink', @mailTo = N'a@example.com', @mailBody = @body;
            """);

        result.AssertNoErrors();
        Assert.Contains("<caption", mail.Sink.Single().Message.HtmlBody);
    }

    [Fact]
    public async Task Options_become_headers()
    {
        Assert.SkipWhen(SendMailFixture.Server == null, Skip);

        var result = await mail.SendAsync("""
            @profileName = N'sink', @mailTo = N'a@example.com', @mailBody = N'Plain text', @bodyHtml = 0,
            @fromAddress = N'other@example.com', @displayName = N'Other', @requestReadReceipt = 1, @sensitivity = 2, @mailPriorty = 2
            """);

        result.AssertNoErrors();
        var message = mail.Sink.Single().Message;
        Assert.Equal("other@example.com", message.From.Mailboxes.Single().Address);
        Assert.Equal("Other", message.From.Mailboxes.Single().Name);
        Assert.Equal("other@example.com", message.Headers["Disposition-Notification-To"]);
        Assert.Equal("Company-Confidential", message.Headers["Sensitivity"]);
        Assert.Equal(MimeKit.MessageImportance.High, message.Importance);
        Assert.Equal("Plain text", message.TextBody?.Trim());
        Assert.Null(message.HtmlBody);
    }

    [Fact]
    public async Task A_file_is_attached_and_saved()
    {
        Assert.SkipWhen(SendMailFixture.Server == null, Skip);
        var path = Path.Combine(mail.Folder, "report.csv");
        await File.WriteAllTextAsync(path, "a,b\r\n1,2\r\n", TestContext.Current.CancellationToken);

        var result = await mail.SendAsync($"@profileName = N'sink', @mailTo = N'a@example.com', @mailBody = N'See the file', @fileAttachments = {Sql.Literal(path)}, @configName = N'test'");

        result.AssertNoErrors();
        var attachment = Assert.Single(mail.Sink.Single().Message.Attachments.OfType<MimeKit.MimePart>());
        Assert.Equal("report.csv", attachment.FileName);
        using var content = new MemoryStream();
        await attachment.Content!.DecodeToAsync(content, TestContext.Current.CancellationToken);
        Assert.Equal("a,b\r\n1,2\r\n", Encoding.UTF8.GetString(content.ToArray()));

        // Version 1 saved an attachment of zeros, as long as the file
        var saved = await mail.RunAsync("SELECT TOP (1) filename, CAST(attachment AS varchar(max)) FROM EMAIL.MailAttachments ORDER BY attachment_id DESC;");
        Assert.Equal(["report.csv", "a,b\r\n1,2\r\n"], Assert.Single(saved.Table.Rows));
    }

    [Theory]
    [InlineData("outside", "is not in the configuration's AttachmentFolders.")]
    [InlineData("parent", "is not in the configuration's AttachmentFolders.")]
    [InlineData("tool.exe", "has a prohibited extension.")]
    [InlineData("missing.txt", "does not exist, or the SQL Server service account can't see it.")]
    [InlineData("big.txt", "has 2000 bytes, more than the configuration's MaxFileSize of 1000.")]
    [InlineData("relative", "is not a full path.")]
    public async Task An_attachment_outside_the_rules_stops_the_message(string file, string message)
    {
        Assert.SkipWhen(SendMailFixture.Server == null, Skip);
        var path = file switch
        {
            "outside" => Path.Combine(mail.Root, "outside.txt"),
            "parent" => Path.Combine(mail.Folder, "..", "parent.txt"),
            "relative" => "report.csv",
            _ => Path.Combine(mail.Folder, file)
        };
        if (file is "outside" or "parent" or "tool.exe")
            await File.WriteAllTextAsync(Path.GetFullPath(path), "x", TestContext.Current.CancellationToken);
        if (file == "big.txt")
            await File.WriteAllTextAsync(path, new string('x', 2000), TestContext.Current.CancellationToken);

        var result = await mail.SendAsync($"@profileName = N'sink', @mailTo = N'a@example.com', @mailBody = N'x', @fileAttachments = {Sql.Literal(path)}, @configName = N'test'");

        Assert.EndsWith(message, Assert.Single(result.Errors).Message);
        Assert.Empty(mail.Sink.Deliveries);
    }

    [Fact]
    public async Task The_built_in_configuration_allows_no_attachments()
    {
        Assert.SkipWhen(SendMailFixture.Server == null, Skip);

        var result = await mail.SendAsync($"@profileName = N'sink', @mailTo = N'a@example.com', @mailBody = N'x', @fileAttachments = {Sql.Literal(Path.Combine(mail.Folder, "report.csv"))}");

        Assert.StartsWith("The configuration 'built-in' allows no attachments", Assert.Single(result.Errors).Message);
    }

    [Theory]
    [InlineData("@profileName = N'nobody', @mailTo = N'a@example.com', @mailBody = N'x'", "The profile 'nobody' is not in EMAIL.Profiles. Add it with EMAIL.SaveProfile.")]
    [InlineData("@profileName = N'sink', @mailTo = NULL, @mailBody = N'x'", "Parameter @mailTo must be specified!")]
    [InlineData("@profileName = N'sink', @mailTo = N'a@example.com', @mailBody = N' '", "Parameter @mailBody must be specified!")]
    [InlineData("@profileName = N'sink', @mailTo = N'not an address', @mailBody = N'x'", "'not an address' is not an e-mail address.")]
    [InlineData("@profileName = N'sink', @mailTo = N'a@example.com', @mailBody = N'x', @mailPriorty = 5", "@mailPriorty is 5: use 0 normal, 1 low, 2 high.")]
    [InlineData("@profileName = N'sink', @mailTo = N'a@example.com', @mailBody = N'x', @configName = N'nothing'", "The configuration 'nothing' is not in EMAIL.Configurations.")]
    public async Task A_mistake_is_an_error_and_sends_nothing(string arguments, string message)
    {
        Assert.SkipWhen(SendMailFixture.Server == null, Skip);

        var result = await mail.RunAsync($"EXEC EMAIL.CLRSendMail {arguments}; SELECT @@ERROR;");

        var error = Assert.Single(result.Errors);
        Assert.Equal(message, error.Message);
        Assert.Equal("50000", result.Value);
        Assert.DoesNotContain(mail.Sink.Deliveries, d => d.Message.TextBody?.Trim() == "x");
    }

    [Fact]
    public async Task A_server_that_is_not_there_is_an_error_in_the_log()
    {
        Assert.SkipWhen(SendMailFixture.Server == null, Skip);

        var result = await mail.SendAsync("@profileName = N'closed', @mailTo = N'a@example.com', @mailBody = N'x'");

        // Version 1 sent in the background by default, so a failure went unnoticed
        var error = Assert.Single(result.Errors);
        Assert.StartsWith("The e-mail was not sent: Failure sending mail.", error.Message);
        var logged = await mail.RunAsync("SELECT TOP (1) Type, Message FROM EMAIL.MonitorLog ORDER BY ID DESC;");
        Assert.Equal(["Error", "E-mail was not sent!"], Assert.Single(logged.Table.Rows));
    }

    [Fact]
    public async Task A_profile_with_ssl_needs_a_server_that_offers_tls()
    {
        Assert.SkipWhen(SendMailFixture.Server == null, Skip);

        var result = await mail.SendAsync("@profileName = N'tls', @mailTo = N'a@example.com', @mailBody = N'x'");

        Assert.Contains("Server does not support secure connections.", Assert.Single(result.Errors).Message);
        Assert.Empty(mail.Sink.Deliveries);
    }

    [Fact]
    public async Task A_caller_needs_only_EXECUTE()
    {
        Assert.SkipWhen(SendMailFixture.Server == null, Skip);
        mail.Sink.Clear();

        var result = await mail.RunAsync("""
            IF DATABASE_PRINCIPAL_ID(N'Caller') IS NULL
                CREATE USER Caller WITHOUT LOGIN;
            GRANT EXECUTE ON EMAIL.CLRSendMail TO Caller;
            EXECUTE AS USER = N'Caller';
            EXEC EMAIL.CLRSendMail @profileName = N'sink', @mailTo = N'a@example.com', @mailBody = N'From a caller';
            SELECT HAS_PERMS_BY_NAME(N'EMAIL.Profiles', N'OBJECT', N'SELECT');
            REVERT;
            """);

        result.AssertNoErrors();
        Assert.Equal("0", result.Value);
        Assert.Equal(SendMailFixture.Password, mail.Sink.Single().Password);
    }

    [Fact]
    public async Task A_password_is_kept_encrypted_and_saving_without_one_keeps_it()
    {
        Assert.SkipWhen(SendMailFixture.Server == null, Skip);

        var stored = await mail.RunAsync("SELECT CONVERT(varchar(max), Password, 1) FROM EMAIL.Profiles WHERE ProfileName = 'sink';");
        var plain = Convert.ToHexString(Encoding.Unicode.GetBytes(SendMailFixture.Password));
        Assert.DoesNotContain(plain, stored.Value);

        var result = await mail.RunAsync($"""
            EXEC EMAIL.SaveProfile @profileName = N'sink', @hostName = N'127.0.0.1', @port = {mail.Sink.Port}, @enableSsl = 0,
                 @userName = N'mailer', @defaultFrom = N'sql@example.com', @defaultDisplayName = N'SQL Server';
            SELECT CONVERT(nvarchar(120), DECRYPTBYCERT(CERT_ID(N'EmailProfiles'), Password)) FROM EMAIL.Profiles WHERE ProfileName = 'sink';
            """);

        Assert.Equal(SendMailFixture.Password, result.Value);
    }

    [Fact]
    public async Task A_password_too_long_to_encrypt_is_refused()
    {
        Assert.SkipWhen(SendMailFixture.Server == null, Skip);

        var result = await mail.RunAsync($"EXEC EMAIL.SaveProfile @profileName = N'long', @hostName = N'127.0.0.1', @defaultFrom = N'sql@example.com', @password = N'{new string('x', 121)}';");

        Assert.Equal("@password is longer than 120 characters, the most the certificate EmailProfiles can encrypt.", Assert.Single(result.Errors).Message);
    }

    [Fact]
    public async Task Version_1_profiles_are_upgraded()
    {
        Assert.SkipWhen(SendMailFixture.Server == null, Skip);
        var database = "SendMailV1_" + Guid.NewGuid().ToString("N")[..8];
        var connection = mail.ConnectionString(database);
        await Sql.RunAsync(mail.ConnectionString("master"), $"CREATE DATABASE {database};");
        try
        {
            // Version 1's tables, with credentials encrypted the way its WPF utility did
            (await Sql.RunAsync(connection, $"""
                EXEC (N'CREATE SCHEMA EMAIL');
                CREATE TABLE EMAIL.Profiles ([ProfileName ] char(20) NOT NULL PRIMARY KEY, EnableSsl bit NOT NULL DEFAULT 1, DefaultCred bit NOT NULL DEFAULT 0,
                    HostName nvarchar(50) NOT NULL DEFAULT 'smtp.gmail.com', Port int NOT NULL DEFAULT 587, UserName nvarchar(200) NULL, Password nvarchar(200) NULL,
                    DOMAIN nvarchar(200) NULL, DefaultFrom nvarchar(500) NOT NULL, DefaultGroup nvarchar(100) NULL, DeliveryMethod tinyint NOT NULL DEFAULT 0, TimeOut int NOT NULL DEFAULT 100000);
                CREATE TABLE EMAIL.Configurations (Name char(20) NOT NULL PRIMARY KEY, MaxFileSize int NOT NULL DEFAULT 1000000, ProhibitedExtensions nvarchar(50) NOT NULL DEFAULT 'exe,dll,vbs,js',
                    LoggingLevel tinyint NOT NULL DEFAULT 0, SaveEmails bit NOT NULL DEFAULT 1, SendAsync bit NOT NULL DEFAULT 1, NoPiping bit NOT NULL DEFAULT 1, SaveAttachments bit NOT NULL DEFAULT 1);
                INSERT INTO EMAIL.Configurations (Name) VALUES ('default');
                INSERT INTO EMAIL.Profiles ([ProfileName ], UserName, Password, DefaultFrom)
                VALUES ('SimpleTalk', {Sql.Literal(Version1.Encrypt("TestAccount@gmail.com"))}, {Sql.Literal(Version1.Encrypt("secret"))}, 'sql@example.com'),
                       ('Placeholder', N'<your encrypted login name>', N'<your encrypted password>', 'sql@example.com');
                """)).AssertNoErrors();

            var setup = await Sql.RunScriptAsync(connection, Repository.SetupSendMail);

            setup.AssertNoErrors();
            Assert.Contains("The profile 'SimpleTalk' now keeps its password encrypted with the certificate EmailProfiles.", setup.Messages);
            Assert.Contains(setup.Messages, m => m.StartsWith("The profile 'Placeholder' has a user name or password that version 1 couldn't decrypt either"));
            var profile = await Sql.RunAsync(connection,
                "SELECT UserName, CONVERT(nvarchar(120), DECRYPTBYCERT(CERT_ID(N'EmailProfiles'), Password)) FROM EMAIL.Profiles WHERE ProfileName = 'SimpleTalk';");
            Assert.Equal(["TestAccount@gmail.com", "secret"], Assert.Single(profile.Table.Rows));
            var columns = await Sql.RunAsync(connection,
                "SELECT COUNT(*) FROM sys.columns WHERE name IN (N'UserNameV1', N'PasswordV1') OR (object_id = OBJECT_ID(N'EMAIL.Configurations') AND name = N'AttachmentFolders');");
            Assert.Equal("1", columns.Value);
        }
        finally
        {
            await Sql.RunScriptAsync(connection, Repository.CleanUp);
            await Sql.RunAsync(mail.ConnectionString("master"), $"ALTER DATABASE {database} SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE {database};");
        }
    }

    /// <summary>Version 1's encryption of credentials: TripleDES keyed from its published secret word.</summary>
    private static class Version1
    {
        public static string Encrypt(string text)
        {
            using var tripleDes = TripleDES.Create();
            tripleDes.Key = Truncate(SHA1.HashData(Encoding.Unicode.GetBytes("SimpleTalk")), tripleDes.KeySize / 8);
            tripleDes.IV = Truncate(SHA1.HashData([]), tripleDes.BlockSize / 8);
            var plain = Encoding.Unicode.GetBytes(text);
            return Convert.ToBase64String(tripleDes.CreateEncryptor().TransformFinalBlock(plain, 0, plain.Length));
        }

        private static byte[] Truncate(byte[] hash, int length)
        {
            Array.Resize(ref hash, length);
            return hash;
        }
    }
}
