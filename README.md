# SQLCLR: e-mail from SQL Server

Two SQLCLR parts for e-mail from SQL Server:

- `EMAIL.QueryToHtml` turns query results into styled HTML tables, and `EMAIL.ConCatHtml` joins two of those documents into one.
- `EMAIL.CLRSendMail` sends a message through an SMTP server, as a replacement for `sp_send_dbmail`.

The Red Gate Simple Talk article [SQLCLR in Practice: Creating a Better Way of Sending Email from SQL Server](https://www.red-gate.com/simple-talk/sql/sql-development/sqlclr-practice-creating-better-way-sending-email-sql-server/) explains the idea. The code as the article shows it is the [`v1-article`](https://github.com/Darko-Martinovic/SQLCLR/tree/v1-article) tag. The `main` branch is version 2, for current SQL Server.

## The two parts

| Part | Setup script | Assembly | Runs on |
|---|---|---|---|
| `QueryToHtml`, `ConCatHtml`, `CustomSendMailHelp` | [Setup/SetupHtml.sql](Setup/SetupHtml.sql) | SAFE | SQL Server 2017+ on Windows or Linux |
| `CLRSendMail`, `SaveProfile` | [Setup/SetupSendMail.sql](Setup/SetupSendMail.sql) | UNSAFE | SQL Server 2017+ on Windows |

The HTML part is useful on its own. Its output can go out through Database Mail (`msdb.dbo.sp_send_dbmail`), which also runs on Linux and on Azure SQL Managed Instance.

`CLRSendMail` needs an UNSAFE assembly. SQL Server on Linux runs SAFE assemblies only, and .NET Framework's `SmtpClient` needs full trust for any port but 25.

## Version 2

The functions and the procedure keep their names and parameters. These have changed:

- **Installing:**
  - Each setup script adds its assembly to the server's trusted assemblies (`sys.sp_add_trusted_assembly`, SQL Server 2017+). It no longer needs SQLCMD mode, five variables to edit, or a key file copied to the server.
  - Version 1's key file was published in this repository. The server gave UNSAFE ASSEMBLY to a login made from it, so it trusted any assembly signed with that file. SetupSendMail.sql drops that key and login once no database uses version 1.
  - Version 1's Setup.sql was from before `QueryToHtml` existed, so the README's examples didn't run, and it ended by enabling a DDL trigger only the author's database had.
- **Passwords:**
  - Version 1 encrypted them with a key derived from a word in its source code, so anyone who could read `EMAIL.Profiles` could decrypt them.
  - They are now encrypted with a certificate in the database (`ENCRYPTBYCERT`), and only `CLRSendMail` decrypts them. It runs as the schema's owner, so callers need EXECUTE on it and nothing more.
  - `EMAIL.SaveProfile` adds and changes profiles. SetupSendMail.sql re-encrypts version 1's profiles.
- **Sending:**
  - The server's TLS certificate is checked. Version 1 accepted any certificate.
  - TLS 1.2 is turned on where .NET Framework is pinned to older versions; otherwise Windows chooses the version.
  - Mail is sent while the caller waits. A failure is error 50000, logged in `EMAIL.MonitorLog`. Version 1 sent in the background by default and reported nothing.
  - `@replyAddress` works; version 1 ignored it.
  - The built-in profiles `ssl` and `simple`, with their hard-coded servers, are gone.
- **Attachments:**
  - Files may be attached only from the folders a configuration allows in its new column `AttachmentFolders`. Version 1 attached any file the SQL Server service account could read.
  - A file that can't be attached stops the message instead of being left out.
  - Saved attachments hold the file. Version 1 saved zeros.
- **QueryToHtml:**
  - `@Params` is a `DECLARE` list, which SQL Server parses. Version 1's own parser:
    - turned `bigint` values into NULL
    - padded `varchar`
    - failed on `decimal(p,s)`
    - failed on times after 12:59
  - Queries are no longer split at semicolons, so a semicolon inside a string works.
  - Every value, caption and footer is HTML-escaped.
  - Values look the same in any session language.
  - The footer's row count no longer includes the header.
  - Errors are SQL Server errors. Version 1 returned the error message as the HTML, which then went out by e-mail.
- **Styles:**
  - The built-in styles are written into each element's `style` attribute, because some e-mail clients drop a style sheet in the head.
  - `ConCatHtml` keeps tables of custom styles, which version 1 dropped.
- **The code:**
  - It is built with `dotnet build` instead of an SSDT project.
  - Tests cover the HTML part on SQL Server 2022 and 2025 for Linux, and the sending part on SQL Server on Windows.

## Requirements

- SQL Server 2017 or later. Azure SQL Database runs no CLR assemblies; Azure SQL Managed Instance runs them, but the scripts haven't been tested there.
- CLR enabled on the server.
- A member of `sysadmin` to run the setup scripts, since they change what the server trusts.

SQL Server runs CLR assemblies on .NET Framework 4.x only, even in SQL Server 2025, so the assemblies target .NET Framework 4.8. The tests and the build tool use .NET 10.

## Install

1. Enable CLR, if it isn't:

   ```sql
   EXEC sp_configure 'clr enabled', 1;
   RECONFIGURE;
   ```

2. Open [Setup/SetupHtml.sql](Setup/SetupHtml.sql) in SSMS, choose your database and run it.
3. On Windows, to send with `CLRSendMail`, also run [Setup/SetupSendMail.sql](Setup/SetupSendMail.sql) in the same database. It creates:
   - the tables `EMAIL.Profiles`, `EMAIL.Configurations`, `EMAIL.MonitorLog`, `EMAIL.MailItems` and `EMAIL.MailAttachments`, with a configuration called `default`
   - the certificate `EmailProfiles`, and the database master key if there is none
4. Add a profile:

   ```sql
   EXEC EMAIL.SaveProfile @profileName = N'SimpleTalk',
                          @hostName = N'smtp.example.com',
                          @port = 587,
                          @enableSsl = 1,
                          @userName = N'sql-server@example.com',
                          @password = N'your password',
                          @defaultFrom = N'sql-server@example.com',
                          @defaultDisplayName = N'SQL Server';
   ```

Running a setup script again replaces the installed version and keeps the data.

If SetupSendMail.sql had to create the database master key, it has a random password that nobody knows; SQL Server opens it with the service master key. Before moving the database to another server, back the master key up with a password of your own (`BACKUP MASTER KEY`), or save the profiles again with `EMAIL.SaveProfile` once the database is there.

## Examples

To send e-mail:

```sql
EXEC EMAIL.CLRSendMail @profileName = N'SimpleTalk',
                       @mailTo = N'yourEmail@example.com',
                       @mailSubject = N'First test',
                       @mailBody = N'Mail body',
                       @bodyHtml = 0;
```

To include a query's result in the e-mail body:

```sql
DECLARE @body nvarchar(max) = EMAIL.QueryToHtml(N'SELECT name, database_id FROM sys.databases', N'', N'Databases', N'#', 0, 0, N'ST_BLUE');

EXEC EMAIL.CLRSendMail @profileName = N'SimpleTalk',
                       @mailTo = N'yourEmail@example.com',
                       @mailSubject = N'Test QueryToHtml',
                       @mailBody = @body;
```

To include several query results in the e-mail body:

```sql
DECLARE @body nvarchar(max) = EMAIL.QueryToHtml(N'SELECT name, database_id FROM sys.databases', N'', N'Databases', N'#', 0, 0, N'ST_BLUE');
SET @body = EMAIL.ConCatHtml(@body, EMAIL.QueryToHtml(N'SELECT * FROM EMAIL.Configurations', N'', N'Configurations', N'#', 2, 0, N'ST_RED'));
```

To send the HTML with Database Mail instead, on Windows or Linux:

```sql
EXEC msdb.dbo.sp_send_dbmail @profile_name = N'your Database Mail profile',
                             @recipients = N'yourEmail@example.com',
                             @subject = N'Test QueryToHtml',
                             @body = @body,
                             @body_format = 'HTML';
```

`QueryToHtml` is a function, and SQL Server lets a function run only a few system procedures, such as `sp_who`. For any other procedure, put its result in a temporary table first:

```sql
CREATE TABLE #files (database_name sysname, file_name sysname, size_mb decimal(18, 2));
INSERT INTO #files EXEC sp_executesql N'SELECT DB_NAME(database_id), name, size * 8 / 1024.0 FROM sys.master_files';
DECLARE @body nvarchar(max) = EMAIL.QueryToHtml(N'SELECT * FROM #files', N'', N'Files', N'#', 0, 0, N'ST_BROWN');
```

[Examples/Examples.sql](Examples/Examples.sql) has more examples, including parameters and attachments. `EMAIL.CustomSendMailHelp(N'EMAIL.QueryToHtml')` describes the parameters of each routine.

## QueryToHtml

`EMAIL.QueryToHtml(@query, @Params, @caption, @footer, @rm, @rco, @style)` returns one HTML document with a table for each result of the query.

| Parameter | Meaning |
|---|---|
| `@query` | The query. It may return several results. |
| `@Params` | The query's parameters, declared as in `DECLARE`: `@id int = 1, @name nvarchar(50) = N'Bikes'`. |
| `@caption` | Each table's caption. |
| `@footer` | Each table's footer; `#` gives the number of rows. |
| `@rm` | Rotate mode: 0 no rotation, 1 rotate when there are fewer rows than columns, 2 always rotate. |
| `@rco` | When a table is rotated, the column whose values become the header, counted from 0, or -1 for a Key, Value header. |
| `@style` | `ST_BLUE` (the default), `ST_RED`, `ST_GREEN`, `ST_ROSE`, `ST_BROWN`, `ST_BLACK`, `ST_SIMPLE` or `ST_NOSTYLE`. Or a style element of your own, such as `<style type="text/css">table.report td { color: navy; }</style>`; the tables then take the class of its first `table.name` selector. |

`EMAIL.ConCatHtml(@mHtml, @sHtml)` puts the tables of `@sHtml` after those of `@mHtml`, and adds its style sheet to the head unless `@mHtml` has it already.

## CLRSendMail

| Parameter | Meaning |
|---|---|
| `@profileName` | The profile in `EMAIL.Profiles`. Required. |
| `@mailTo` | A semicolon-delimited list of recipients. Required. |
| `@mailSubject` | Default `SQLCLR Server Message`. |
| `@mailBody` | Required. HTML unless `@bodyHtml = 0`. |
| `@fromAddress`, `@displayName` | Instead of the profile's. |
| `@mailCc`, `@blindCopyRec`, `@replyAddress` | Semicolon-delimited lists. |
| `@fileAttachments` | A semicolon-delimited list of full paths, in a folder of the configuration's `AttachmentFolders`. |
| `@requestReadReceipt` | 1 to ask for a read receipt. |
| `@deliveryNotification` | 0 none, 1 on success, 2 on failure, 4 on delay, or their sum. |
| `@sensitivity` | -1 none, 0 Personal, 1 Private, 2 Company-Confidential. |
| `@mailPriorty` | 0 normal, 1 low, 2 high. |
| `@bodyHtml` | 1 (default) for HTML, 0 for text. |
| `@configName` | The configuration in `EMAIL.Configurations`. When null, a built-in one that allows no attachments. |

A configuration is a row of `EMAIL.Configurations`:

| Column | Meaning |
|---|---|
| `MaxFileSize` | Largest attachment in bytes. |
| `ProhibitedExtensions` | Extensions never attached. |
| `AttachmentFolders` | Folders files may be attached from, semicolon separated. Null allows none. |
| `LoggingLevel` | 2 logs every message sent. |
| `SaveEmails`, `SaveAttachments` | Keep messages and attachments in `EMAIL.MailItems` and `EMAIL.MailAttachments`. |
| `NoPiping` | 0 prints "Sent successfully!". |

Version 1's `SendAsync` column, where it exists, is ignored.

## Sending e-mail from SQL Server today

The SMTP server and its sign-in are what limit `CLRSendMail`, more than SQL Server:

- **Sign-in:**
  - .NET Framework's `SmtpClient` signs in with a user name and password, never with OAuth.
  - Gmail takes an app password, not the account's own.
  - Microsoft 365 turns password sign-in for SMTP off by default from December 2026, per Message Center notice MC786329.
  - SMTP relays that take an API key as the password, and an organisation's own SMTP server, still work.
- **Ports:** `SmtpClient` speaks STARTTLS on 587 or 25, not TLS from the start on 465.
- **Database Mail:** `sp_send_dbmail` has the same SMTP limits, but needs no UNSAFE assembly and runs on Linux too.
- **SQL Server 2025 and Azure SQL:** `sp_invoke_external_rest_endpoint` can call an e-mail service's HTTPS API, with no CLR assembly. In SQL Server 2025 it is off by default (`sp_configure 'external rest endpoint enabled'`). It hasn't been tried with this repository.

## Limitations

- **TRY...CATCH.** T-SQL's TRY...CATCH doesn't catch `CLRSendMail`'s errors; check `@@ERROR`, which is set to 50000. `QueryToHtml`'s errors, as a function's, are error 6522, which TRY...CATCH does catch.
- **Waiting.** The caller waits for the SMTP server, up to the profile's `TimeOut`.
- **Size.** The HTML repeats each built-in style on every cell, so a large result makes a large message.
- **Passwords.** A profile's password is at most 120 characters, about the most a 2048-bit certificate key can encrypt.

## Upgrading from version 1

Run SetupHtml.sql, then SetupSendMail.sql, in each database that has version 1. They:

- drop version 1's routines, assembly and database user `UserSqlClrCustomSendMail`
- keep its tables and data
- re-encrypt the profiles' user names and passwords, and print the profiles they couldn't decrypt
- drop the asymmetric key `keySendMail` and login `SqlClrCustomSendMail` from the server once no database uses version 1, and print a message while one still does

After upgrading:

- To attach files, set `AttachmentFolders` in a configuration.
- Calls that used the built-in profiles `ssl` or `simple` need a profile of their own, made with `EMAIL.SaveProfile`.
- Messages without `@displayName` or a profile display name now have none, instead of "Simple Talk's CLR solution".

## Uninstall

Run [Setup/CleanUp.sql](Setup/CleanUp.sql) in the database, as a sysadmin. It removes everything the setup scripts installed, version 1's included:

- the routines and assemblies
- the tables with their profiles, logs and saved e-mails
- the certificate and the schema
- the server's trust in the assemblies, once no other database uses them

## Building and testing

You need the .NET 10 SDK. The tests also need Docker, or Podman with its Docker API, since Testcontainers starts SQL Server for Linux in a container.

```bash
dotnet build SqlClr.slnx

# SQL Server 2025; set MSSQL_IMAGE=mcr.microsoft.com/mssql/server:2022-latest for 2022
dotnet test --solution SqlClr.slnx
```

The tests of `CLRSendMail` need SQL Server on Windows on the same machine; without one, they are skipped. A LocalDB instance of their own works. The tests enable CLR on it, create and drop a database, and send to a stand-in SMTP server in the test process:

```powershell
SqlLocalDB create SqlClrTests -s
$env:SQLCLR_SENDMAIL_SERVER = 'Server=(localdb)\SqlClrTests;Integrated Security=true;TrustServerCertificate=true'
dotnet test --solution SqlClr.slnx
```

The tests install the committed setup scripts, the files readers run. After changing an assembly's code or a template, generate the scripts again:

```bash
dotnet run -c Release --project tools/SetupScript
```

The scripts record a hash of the sources they were built from, and a test fails when the sources have changed since.

| Path | What it is |
|---|---|
| `SqlClrHtml/` | The SAFE assembly (.NET Framework 4.8): QueryToHtml, ConCatHtml, CustomSendMailHelp |
| `SqlClrSendMail/` | The UNSAFE assembly (.NET Framework 4.8): CLRSendMail |
| `Setup/*.template.sql` | The T-SQL of the setup scripts, without the assemblies |
| `Setup/SetupHtml.sql`, `Setup/SetupSendMail.sql` | Generated: the templates with the assemblies and their hashes |
| `Setup/CleanUp.sql` | Removes the installation |
| `Examples/Examples.sql` | Examples to run in SSMS |
| `tools/SetupScript/` | Generates the setup scripts (.NET 10) |
| `tests/SqlClr.Tests/` | xUnit tests (.NET 10) |
