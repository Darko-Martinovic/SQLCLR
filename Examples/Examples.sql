----------------------------------------------------------
-- Examples to run in SSMS after the setup scripts. Replace
-- the addresses and the SMTP server with your own. The
-- automated tests are in tests/SqlClr.Tests.
----------------------------------------------------------

----------------------------------------------------------
-- A profile: the SMTP server, the account and the from
-- address. The password is kept encrypted with the
-- database's certificate EmailProfiles.
----------------------------------------------------------
EXEC EMAIL.SaveProfile @profileName = N'SimpleTalk',
                       @hostName = N'smtp.example.com',
                       @port = 587,
                       @enableSsl = 1,
                       @userName = N'sql-server@example.com',
                       @password = N'your password',
                       @defaultFrom = N'sql-server@example.com',
                       @defaultDisplayName = N'SQL Server';
GO
----------------------------------------------------------
-- To send e-mail
----------------------------------------------------------
EXEC EMAIL.CLRSendMail @profileName = N'SimpleTalk',
                       @mailTo = N'yourEmail@example.com',
                       @mailSubject = N'First test',
                       @mailBody = N'Mail body',
                       @bodyHtml = 0;
GO
----------------------------------------------------------
-- To include a query's result in the e-mail body
----------------------------------------------------------
DECLARE @body nvarchar(max) = EMAIL.QueryToHtml(N'SELECT name, database_id, create_date FROM sys.databases', N'', N'Databases', N'#', 0, 0, N'ST_BLUE');

EXEC EMAIL.CLRSendMail @profileName = N'SimpleTalk',
                       @mailTo = N'yourEmail@example.com',
                       @mailSubject = N'Test QueryToHtml',
                       @mailBody = @body;
GO
----------------------------------------------------------
-- To include several query results in the e-mail body
----------------------------------------------------------
DECLARE @body nvarchar(max) = EMAIL.QueryToHtml(N'SELECT name, database_id FROM sys.databases', N'', N'Databases', N'#', 0, 0, N'ST_BLUE');
SET @body = EMAIL.ConCatHtml(@body, EMAIL.QueryToHtml(N'SELECT * FROM EMAIL.Configurations', N'', N'Configurations', N'#', 2, 0, N'ST_RED'));

EXEC EMAIL.CLRSendMail @profileName = N'SimpleTalk',
                       @mailTo = N'yourEmail@example.com',
                       @mailSubject = N'Test ConCatHtml',
                       @mailBody = @body;
GO
----------------------------------------------------------
-- With parameters, declared as in DECLARE
----------------------------------------------------------
DECLARE @body nvarchar(max) = EMAIL.QueryToHtml(N'SELECT name, create_date FROM sys.databases WHERE create_date >= @since',
                                                N'@since datetime2 = ''2020-01-01''', N'Recent databases', N'#', 0, 0, N'ST_GREEN');
SELECT @body;
GO
----------------------------------------------------------
-- A stored procedure's result: QueryToHtml is a function,
-- and SQL Server lets a function run only a few system
-- procedures, so put the result in a temporary table first
----------------------------------------------------------
CREATE TABLE #files (database_name sysname, file_name sysname, size_mb decimal(18, 2));
INSERT INTO #files EXEC sp_executesql N'SELECT DB_NAME(database_id), name, size * 8 / 1024.0 FROM sys.master_files';

DECLARE @body nvarchar(max) = EMAIL.QueryToHtml(N'SELECT * FROM #files ORDER BY size_mb DESC', N'', N'Files', N'#', 0, 0, N'ST_BROWN');
SELECT @body;
DROP TABLE #files;
GO
----------------------------------------------------------
-- With attachments: allow a folder in a configuration
-- first, and pass that configuration
----------------------------------------------------------
UPDATE EMAIL.Configurations SET AttachmentFolders = N'C:\Reports' WHERE Name = 'default';

EXEC EMAIL.CLRSendMail @profileName = N'SimpleTalk',
                       @mailTo = N'yourEmail@example.com',
                       @mailSubject = N'Test attachments',
                       @mailBody = N'The report is attached.',
                       @bodyHtml = 0,
                       @fileAttachments = N'C:\Reports\report.csv',
                       @configName = N'default';
GO
----------------------------------------------------------
-- The same HTML through Database Mail, which also runs on
-- Linux (needs a Database Mail profile in msdb)
----------------------------------------------------------
DECLARE @body nvarchar(max) = EMAIL.QueryToHtml(N'SELECT name, database_id FROM sys.databases', N'', N'Databases', N'#', 0, 0, N'ST_BLUE');

EXEC msdb.dbo.sp_send_dbmail @profile_name = N'your Database Mail profile',
                             @recipients = N'yourEmail@example.com',
                             @subject = N'Test QueryToHtml with Database Mail',
                             @body = @body,
                             @body_format = 'HTML';
GO
----------------------------------------------------------
-- The parameters
----------------------------------------------------------
SELECT * FROM EMAIL.CustomSendMailHelp(N'EMAIL.CLRSendMail');
SELECT * FROM EMAIL.CustomSendMailHelp(N'EMAIL.SaveProfile');
SELECT * FROM EMAIL.CustomSendMailHelp(N'EMAIL.QueryToHtml');
SELECT * FROM EMAIL.CustomSendMailHelp(N'EMAIL.ConCatHtml');
GO
