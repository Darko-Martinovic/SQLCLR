-------------------------------------------------------------------------
-- Removes everything SetupHtml.sql and SetupSendMail.sql installed in the
-- current database, of version 1 or 2: the routines, the assemblies, the
-- tables with their profiles, logs and saved e-mails, the certificate and
-- the schema. Then the server's trust in the assemblies, once no other
-- database uses them; run it as a sysadmin for that part.
-------------------------------------------------------------------------
SET NOCOUNT ON;
GO
-------------------------------------------------------------------------
-- The routines and the assemblies
-------------------------------------------------------------------------
DECLARE @drop nvarchar(max) =
(
    SELECT STRING_AGG(CAST(N'DROP ' + CASE o.type WHEN 'PC' THEN N'PROCEDURE ' ELSE N'FUNCTION ' END
                      + QUOTENAME(SCHEMA_NAME(o.schema_id)) + N'.' + QUOTENAME(o.name) + N';' AS nvarchar(max)), NCHAR(10))
    FROM sys.assembly_modules AS m
         JOIN sys.objects AS o ON o.object_id = m.object_id
         JOIN sys.assemblies AS a ON a.assembly_id = m.assembly_id
    WHERE a.name IN (N'SimpleTalk.SQLCLR.SendMail', N'SimpleTalk.SQLCLR.Html')
);
IF @drop IS NOT NULL
    EXEC sys.sp_executesql @drop;
IF EXISTS (SELECT * FROM sys.assemblies WHERE name = N'SimpleTalk.SQLCLR.SendMail')
    DROP ASSEMBLY [SimpleTalk.SQLCLR.SendMail];
IF EXISTS (SELECT * FROM sys.assemblies WHERE name = N'SimpleTalk.SQLCLR.Html')
    DROP ASSEMBLY [SimpleTalk.SQLCLR.Html];
IF OBJECT_ID(N'EMAIL.SaveProfile', N'P') IS NOT NULL
    DROP PROCEDURE EMAIL.SaveProfile;
-- Version 1 made the assembly's owner a user of its own
IF DATABASE_PRINCIPAL_ID(N'UserSqlClrCustomSendMail') IS NOT NULL
    DROP USER [UserSqlClrCustomSendMail];
GO
-------------------------------------------------------------------------
-- The tables, version 1's table type, the certificate and the schema
-------------------------------------------------------------------------
IF OBJECT_ID(N'EMAIL.MailAttachments', N'U') IS NOT NULL
    DROP TABLE EMAIL.MailAttachments;
IF OBJECT_ID(N'EMAIL.MailItems', N'U') IS NOT NULL
    DROP TABLE EMAIL.MailItems;
IF OBJECT_ID(N'EMAIL.Configurations', N'U') IS NOT NULL
    DROP TABLE EMAIL.Configurations;
IF OBJECT_ID(N'EMAIL.Profiles', N'U') IS NOT NULL
    DROP TABLE EMAIL.Profiles;
IF OBJECT_ID(N'EMAIL.MonitorLog', N'U') IS NOT NULL
    DROP TABLE EMAIL.MonitorLog;
IF TYPE_ID(N'EMAIL.TVP_Emails') IS NOT NULL
    DROP TYPE EMAIL.TVP_Emails;
IF CERT_ID(N'EmailProfiles') IS NOT NULL
    DROP CERTIFICATE EmailProfiles;
IF SCHEMA_ID(N'EMAIL') IS NOT NULL
BEGIN
    IF EXISTS (SELECT * FROM sys.objects WHERE schema_id = SCHEMA_ID(N'EMAIL'))
       OR EXISTS (SELECT * FROM sys.types WHERE schema_id = SCHEMA_ID(N'EMAIL'))
        PRINT N'The schema EMAIL still holds objects of yours, so it stays.';
    ELSE
        EXEC sys.sp_executesql N'DROP SCHEMA [EMAIL];';
END;
GO
-------------------------------------------------------------------------
-- The server's trust, once no database uses the assemblies: version 2's
-- trusted assemblies, version 1's key and login
-------------------------------------------------------------------------
IF IS_SRVROLEMEMBER(N'sysadmin') = 0
BEGIN
    PRINT N'Only a sysadmin can remove the server''s trust in the assemblies: run CleanUp.sql as one to finish.';
    RETURN;
END;

-- The builds of the assemblies each database has, version 1's signed with the published key
DECLARE @used TABLE (hash varbinary(64), clr_name nvarchar(4000));
DECLARE @sql nvarchar(max) =
(
    SELECT STRING_AGG(CAST(N'SELECT HASHBYTES(''SHA2_512'', f.content), a.clr_name COLLATE DATABASE_DEFAULT FROM ' + QUOTENAME(name) + N'.sys.assemblies AS a JOIN '
                           + QUOTENAME(name) + N'.sys.assembly_files AS f ON f.assembly_id = a.assembly_id AND f.file_id = 1'
                           + N' WHERE a.name IN (N''SimpleTalk.SQLCLR.SendMail'', N''SimpleTalk.SQLCLR.Html'')' AS nvarchar(max)), N' UNION ALL ')
    FROM sys.databases
    WHERE state = 0 AND HAS_DBACCESS(name) = 1
);
INSERT INTO @used EXEC sys.sp_executesql @sql;

-- sys.trusted_assemblies came with SQL Server 2017, where version 1 may still be installed
IF ISNULL(CAST(SERVERPROPERTY('ProductMajorVersion') AS int), 0) >= 14
BEGIN
    DECLARE @trusted TABLE (hash varbinary(64));
    INSERT INTO @trusted
    EXEC sys.sp_executesql N'SELECT hash FROM sys.trusted_assemblies
                             WHERE description LIKE N''simpletalk.sqlclr.sendmail,%'' OR description LIKE N''simpletalk.sqlclr.html,%''';
    DECLARE @stale nvarchar(max) =
    (
        SELECT STRING_AGG(CAST(N'EXEC sys.sp_drop_trusted_assembly @hash = ' + CONVERT(nvarchar(max), t.hash, 1) + N';' AS nvarchar(max)), NCHAR(10))
        FROM @trusted AS t
        WHERE NOT EXISTS (SELECT * FROM @used AS u WHERE u.hash = t.hash)
    );
    IF @stale IS NOT NULL
        EXEC sys.sp_executesql @stale;
END;

IF EXISTS (SELECT * FROM @used WHERE clr_name LIKE N'simpletalk.sqlclr.sendmail,%' AND clr_name NOT LIKE N'%publickeytoken=null%')
    PRINT N'Version 1 is still installed in another database, so its key keySendMail and login SqlClrCustomSendMail stay.';
ELSE
BEGIN
    IF EXISTS (SELECT * FROM sys.server_principals WHERE name = N'SqlClrCustomSendMail')
        DROP LOGIN [SqlClrCustomSendMail];
    IF EXISTS (SELECT * FROM master.sys.asymmetric_keys WHERE name = N'keySendMail')
        EXEC master.sys.sp_executesql N'DROP ASYMMETRIC KEY [keySendMail];';
END;
GO
