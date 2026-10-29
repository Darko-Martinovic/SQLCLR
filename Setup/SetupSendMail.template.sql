--------------------------------------------------------------------------------------
-- Installs EMAIL.CLRSendMail and EMAIL.SaveProfile in the current database, with the
-- tables they use, replacing an earlier version and keeping its profiles, configurations
-- and logs. Run it in the database you want them in, as a sysadmin: it adds the assembly
-- to the server's trusted assemblies.
--
-- The assembly is UNSAFE: .NET Framework lets SmtpClient use a port other than 25, such as
-- 587, only with full trust, which EXTERNAL_ACCESS lacks. SQL Server runs UNSAFE assemblies
-- on Windows only: SQL Server 2017 or later on Windows, or Azure SQL Managed Instance. On
-- Linux, install SetupHtml.sql and send with Database Mail.
-- Version 1 is the v1-article tag.
--
-- SetupSendMail.sql is generated from SetupSendMail.template.sql by tools/SetupScript; edit the template.
-- Sources: $(SourceHash)
--------------------------------------------------------------------------------------
SET NOCOUNT ON;
GO
--------------------------------------------------------------------------------------
-- Stop here, installing nothing, if the server can't run the assembly
--------------------------------------------------------------------------------------
IF CAST(SERVERPROPERTY('EngineEdition') AS int) = 5
BEGIN
    RAISERROR(N'Azure SQL Database doesn''t run CLR assemblies. CLRSendMail needs SQL Server 2017 or later on Windows, or Azure SQL Managed Instance.', 16, 1);
    SET NOEXEC ON;
END
ELSE IF ISNULL(CAST(SERVERPROPERTY('ProductMajorVersion') AS int), 0) < 14
BEGIN
    RAISERROR(N'CLRSendMail needs SQL Server 2017 or later. For earlier versions, use the v1-article tag of the repository.', 16, 1);
    SET NOEXEC ON;
END
ELSE IF IS_SRVROLEMEMBER(N'sysadmin') = 0
BEGIN
    RAISERROR(N'SetupSendMail.sql adds the assembly to the server''s trusted assemblies, which needs a member of sysadmin.', 16, 1);
    SET NOEXEC ON;
END
ELSE IF EXISTS (SELECT * FROM sys.dm_os_host_info WHERE host_platform = N'Linux')
BEGIN
    RAISERROR(N'SQL Server on Linux runs SAFE assemblies only, and CLRSendMail needs UNSAFE. Install SetupHtml.sql and send its HTML with msdb.dbo.sp_send_dbmail.', 16, 1);
    SET NOEXEC ON;
END
ELSE IF (SELECT value_in_use FROM sys.configurations WHERE name = N'clr enabled') = 0
    PRINT N'CLR is disabled on this server. CLRSendMail is installed, but runs only after: EXEC sp_configure ''clr enabled'', 1; RECONFIGURE;';
GO
--------------------------------------------------------------------------------------
-- Remove an earlier version's code: every routine of the assembly, the assembly, and
-- version 1's database user that owned it. The data stays.
--------------------------------------------------------------------------------------
DECLARE @drop nvarchar(max) =
(
    SELECT STRING_AGG(CAST(N'DROP ' + CASE o.type WHEN 'PC' THEN N'PROCEDURE ' ELSE N'FUNCTION ' END
                      + QUOTENAME(SCHEMA_NAME(o.schema_id)) + N'.' + QUOTENAME(o.name) + N';' AS nvarchar(max)), NCHAR(10))
    FROM sys.assembly_modules AS m
         JOIN sys.objects AS o ON o.object_id = m.object_id
         JOIN sys.assemblies AS a ON a.assembly_id = m.assembly_id
    WHERE a.name = N'SimpleTalk.SQLCLR.SendMail'
);
IF @drop IS NOT NULL
    EXEC sys.sp_executesql @drop;
IF EXISTS (SELECT * FROM sys.assemblies WHERE name = N'SimpleTalk.SQLCLR.SendMail')
    DROP ASSEMBLY [SimpleTalk.SQLCLR.SendMail];
IF DATABASE_PRINCIPAL_ID(N'UserSqlClrCustomSendMail') IS NOT NULL
    DROP USER [UserSqlClrCustomSendMail];
IF OBJECT_ID(N'EMAIL.SaveProfile', N'P') IS NOT NULL
    DROP PROCEDURE EMAIL.SaveProfile;
IF SCHEMA_ID(N'EMAIL') IS NULL
    EXEC sys.sp_executesql N'CREATE SCHEMA EMAIL';
GO
--------------------------------------------------------------------------------------
-- The tables, as version 1 made them, but for the profiles' credentials
--------------------------------------------------------------------------------------
IF OBJECT_ID(N'EMAIL.Profiles', N'U') IS NULL
    CREATE TABLE EMAIL.Profiles
    (
        ProfileName    char(20)       NOT NULL CONSTRAINT PK_Email_Profile_ProfileName PRIMARY KEY,
        EnableSsl      bit            NOT NULL DEFAULT 1,
        DefaultCred    bit            NOT NULL DEFAULT 0,      -- sign in as the SQL Server service account
        HostName       nvarchar(255)  NOT NULL,
        Port           int            NOT NULL DEFAULT 587,
        UserName       nvarchar(200)  NULL,
        Password       varbinary(512) NULL,                    -- ENCRYPTBYCERT(CERT_ID(N'EmailProfiles'), ...)
        Domain         nvarchar(200)  NULL,                    -- Exchange domain name
        DefaultFrom    nvarchar(500)  NOT NULL,                -- default from address
        DefaultGroup   nvarchar(100)  NULL,                    -- default display name
        DeliveryMethod tinyint        NOT NULL DEFAULT 0,      -- 0: an SMTP server
        TimeOut        int            NOT NULL DEFAULT 100000
    );

-- Version 1 kept the user name and password encrypted with a key published with its code:
-- they are moved aside, re-encrypted further down, and dropped
IF EXISTS (SELECT * FROM sys.columns
           WHERE object_id = OBJECT_ID(N'EMAIL.Profiles') AND name = N'Password' AND system_type_id <> TYPE_ID(N'varbinary'))
BEGIN
    EXEC sp_rename N'EMAIL.Profiles.UserName', N'UserNameV1', N'COLUMN';
    EXEC sp_rename N'EMAIL.Profiles.Password', N'PasswordV1', N'COLUMN';
    ALTER TABLE EMAIL.Profiles ADD UserName nvarchar(200) NULL, Password varbinary(512) NULL;
END;

IF OBJECT_ID(N'EMAIL.Configurations', N'U') IS NULL
    CREATE TABLE EMAIL.Configurations
    (
        Name                 char(20)       NOT NULL CONSTRAINT PK_Email_Configurations_Name PRIMARY KEY,
        MaxFileSize          int            NOT NULL DEFAULT 1000000,
        ProhibitedExtensions nvarchar(50)   NOT NULL DEFAULT 'exe,dll,vbs,js',
        LoggingLevel         tinyint        NOT NULL DEFAULT 0, -- 2 logs every message sent
        SaveEmails           bit            NOT NULL DEFAULT 1,
        NoPiping             bit            NOT NULL DEFAULT 1, -- 0 prints "Sent successfully!"
        SaveAttachments      bit            NOT NULL DEFAULT 1,
        AttachmentFolders    nvarchar(1000) NULL                -- folders files may be attached from, semicolon separated
    );

IF COL_LENGTH(N'EMAIL.Configurations', N'AttachmentFolders') IS NULL
    ALTER TABLE EMAIL.Configurations ADD AttachmentFolders nvarchar(1000) NULL;

IF NOT EXISTS (SELECT * FROM EMAIL.Configurations)
    INSERT INTO EMAIL.Configurations (Name) VALUES ('default');

IF OBJECT_ID(N'EMAIL.MonitorLog', N'U') IS NULL
    CREATE TABLE EMAIL.MonitorLog
    (
        ID          bigint        IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Email_MonitorLog_Id PRIMARY KEY,
        [TimeStamp] datetime      NOT NULL DEFAULT (GETDATE()),
        Source      nvarchar(50)  NOT NULL,
        Type        nvarchar(20)  NOT NULL,
        Message     nvarchar(100) NOT NULL,
        Data        nvarchar(max) NULL
    );

IF OBJECT_ID(N'EMAIL.MailItems', N'U') IS NULL
    CREATE TABLE EMAIL.MailItems
    (
        mailitem_id           bigint        IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Email_MailItems_MailId PRIMARY KEY,
        profileName           char(20)      NULL CONSTRAINT FK_MailItems_Profile REFERENCES EMAIL.Profiles (ProfileName),
        configurationName     char(20)      NULL CONSTRAINT FK_MailItems_Configurations REFERENCES EMAIL.Configurations (Name),
        recipients            varchar(max)  NULL,
        copy_recipients       varchar(max)  NULL,
        blind_copy_recipients varchar(max)  NULL,
        subject               nvarchar(255) NULL,
        from_address          varchar(max)  NULL,
        reply_to              varchar(max)  NULL,
        body                  nvarchar(max) NULL,
        body_format           varchar(20)   NULL,
        importance            varchar(6)    NULL,
        sensitivity           varchar(50)   NULL,
        file_attachments      nvarchar(max) NULL,
        last_mod_date         datetime      NOT NULL DEFAULT (GETDATE()),
        last_mod_user         sysname       NOT NULL DEFAULT (SUSER_SNAME())
    );

IF OBJECT_ID(N'EMAIL.MailAttachments', N'U') IS NULL
    CREATE TABLE EMAIL.MailAttachments
    (
        attachment_id bigint         IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Email_MailAttachments_AttachmentId PRIMARY KEY,
        mailitem_id   bigint         NOT NULL CONSTRAINT FK_MailAttachments_MailItems REFERENCES EMAIL.MailItems (mailitem_id),
        filename      nvarchar(512)  NOT NULL,
        filesize      bigint         NOT NULL,
        attachment    varbinary(max) NULL,
        last_mod_date datetime       NOT NULL DEFAULT (GETDATE()),
        last_mod_user sysname        NOT NULL DEFAULT (SUSER_SNAME())
    );
GO
--------------------------------------------------------------------------------------
-- The certificate the profiles' passwords are encrypted with, protected by the database
-- master key. A master key made here has a random password nobody knows: SQL Server opens
-- it with the service master key. Before moving the database to another server, back the
-- master key up with a password of your own (BACKUP MASTER KEY), or save the profiles
-- again with EMAIL.SaveProfile once it's there.
--------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.symmetric_keys WHERE name = N'##MS_DatabaseMasterKey##')
BEGIN
    DECLARE @password nvarchar(128) = CONVERT(nvarchar(36), NEWID()) + N'aA1!' + CONVERT(nvarchar(36), NEWID());
    DECLARE @create nvarchar(max) = N'CREATE MASTER KEY ENCRYPTION BY PASSWORD = ' + QUOTENAME(@password, '''') + N';';
    EXEC sys.sp_executesql @create;
    PRINT N'Created the database master key, which protects the certificate EmailProfiles.';
END;
IF CERT_ID(N'EmailProfiles') IS NULL
    CREATE CERTIFICATE EmailProfiles WITH SUBJECT = N'CLRSendMail profile passwords';
GO
--------------------------------------------------------------------------------------
-- Trust this build of the assembly. SQL Server 2017's "clr strict security" loads only
-- trusted assemblies, and a trusted assembly needs no key, certificate or login.
--
-- Trust that earlier builds needed and no database uses any more is removed:
--   * the hashes of earlier builds of this version
--   * version 1's asymmetric key keySendMail and login SqlClrCustomSendMail, made from a
--     key file published in the repository, so they trusted any assembly signed with it
--------------------------------------------------------------------------------------
DECLARE @hash varbinary(64) = $(AssemblyHash);

-- The builds of the assembly each database has, version 1's signed with the published key
DECLARE @used TABLE (hash varbinary(64), clr_name nvarchar(4000));
DECLARE @sql nvarchar(max) =
(
    SELECT STRING_AGG(CAST(N'SELECT HASHBYTES(''SHA2_512'', f.content), a.clr_name COLLATE DATABASE_DEFAULT FROM ' + QUOTENAME(name) + N'.sys.assemblies AS a JOIN '
                           + QUOTENAME(name) + N'.sys.assembly_files AS f ON f.assembly_id = a.assembly_id AND f.file_id = 1'
                           + N' WHERE a.name = N''SimpleTalk.SQLCLR.SendMail''' AS nvarchar(max)), N' UNION ALL ')
    FROM sys.databases
    WHERE state = 0 AND HAS_DBACCESS(name) = 1
);
INSERT INTO @used EXEC sys.sp_executesql @sql;

DECLARE @stale nvarchar(max) =
(
    SELECT STRING_AGG(CAST(N'EXEC sys.sp_drop_trusted_assembly @hash = ' + CONVERT(nvarchar(max), t.hash, 1) + N';' AS nvarchar(max)), NCHAR(10))
    FROM sys.trusted_assemblies AS t
    WHERE t.description LIKE N'simpletalk.sqlclr.sendmail,%'
          AND t.hash <> @hash
          AND NOT EXISTS (SELECT * FROM @used AS u WHERE u.hash = t.hash)
);
IF @stale IS NOT NULL
    EXEC sys.sp_executesql @stale;

IF NOT EXISTS (SELECT * FROM sys.trusted_assemblies WHERE hash = @hash)
    EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'$(AssemblyDescription)';

IF EXISTS (SELECT * FROM sys.server_principals WHERE name = N'SqlClrCustomSendMail')
   OR EXISTS (SELECT * FROM master.sys.asymmetric_keys WHERE name = N'keySendMail')
BEGIN
    IF EXISTS (SELECT * FROM @used WHERE clr_name NOT LIKE N'%publickeytoken=null%')
        PRINT N'Version 1 is still installed in another database, so its key keySendMail and login SqlClrCustomSendMail stay. They trust any assembly signed with a key file anyone can download: run SetupSendMail.sql or CleanUp.sql in that database too.';
    ELSE
    BEGIN
        IF EXISTS (SELECT * FROM sys.server_principals WHERE name = N'SqlClrCustomSendMail')
            DROP LOGIN [SqlClrCustomSendMail];
        IF EXISTS (SELECT * FROM master.sys.asymmetric_keys WHERE name = N'keySendMail')
            EXEC master.sys.sp_executesql N'DROP ASYMMETRIC KEY [keySendMail];';
        PRINT N'Removed version 1''s key keySendMail and login SqlClrCustomSendMail.';
    END;
END;
GO
--------------------------------------------------------------------------------------
-- The assembly and its procedure. CLRSendMail runs as the schema's owner, who may decrypt
-- the profiles' passwords: callers need EXECUTE on it, not access to the certificate.
--------------------------------------------------------------------------------------
CREATE ASSEMBLY [SimpleTalk.SQLCLR.SendMail]
    AUTHORIZATION [dbo]
    FROM $(AssemblyBits)
    WITH PERMISSION_SET = UNSAFE;
GO
CREATE PROCEDURE [EMAIL].[CLRSendMail]
    @profileName [nvarchar](20),
    @mailTo [nvarchar](max),
    @mailSubject [nvarchar](255) = N'SQLCLR Server Message',
    @mailBody [nvarchar](max),
    @fromAddress [nvarchar](500) = NULL,
    @displayName [nvarchar](400) = NULL,
    @mailCc [nvarchar](4000) = NULL,
    @blindCopyRec [nvarchar](4000) = NULL,
    @replyAddress [nvarchar](4000) = NULL,
    @fileAttachments [nvarchar](4000) = NULL,
    @requestReadReceipt [bit] = 0,
    @deliveryNotification [smallint] = 0,
    @sensitivity [smallint] = -1,
    @mailPriorty [smallint] = 0,
    @bodyHtml [bit] = 1,
    @configName [nvarchar](20) = NULL
WITH EXECUTE AS OWNER
AS EXTERNAL NAME [SimpleTalk.SQLCLR.SendMail].[SimpleTalk.SqlClr.SendMail.StoredProcedures].[CLRSendMail];
GO
--------------------------------------------------------------------------------------
-- Adds or replaces a profile, with its password encrypted by the certificate EmailProfiles
--------------------------------------------------------------------------------------
CREATE PROCEDURE [EMAIL].[SaveProfile]
    @profileName nvarchar(20),
    @hostName nvarchar(255),
    @defaultFrom nvarchar(500),
    @port int = 587,
    @enableSsl bit = 1,
    @userName nvarchar(200) = NULL,
    @password nvarchar(4000) = NULL,        -- null keeps the profile's password; N'' removes it
    @defaultDisplayName nvarchar(100) = NULL,
    @useDefaultCredentials bit = 0,
    @domain nvarchar(200) = NULL,
    @timeout int = 100000
AS
BEGIN
    SET NOCOUNT ON;
    IF @profileName IS NULL OR LTRIM(@profileName) = N'' OR @hostName IS NULL OR @defaultFrom IS NULL
    BEGIN
        RAISERROR(N'@profileName, @hostName and @defaultFrom are required.', 16, 1);
        RETURN;
    END;

    -- A certificate's RSA key encrypts at most 245 bytes when it is 2048 bits long
    IF LEN(@password) > 120
    BEGIN
        RAISERROR(N'@password is longer than 120 characters, the most the certificate EmailProfiles can encrypt.', 16, 1);
        RETURN;
    END;

    DECLARE @encrypted varbinary(512) = CASE WHEN @password IS NULL OR @password = N'' THEN NULL
                                             ELSE ENCRYPTBYCERT(CERT_ID(N'EmailProfiles'), @password) END;
    IF @password <> N'' AND @encrypted IS NULL
    BEGIN
        RAISERROR(N'The password can''t be encrypted: the certificate EmailProfiles is missing. Run SetupSendMail.sql again.', 16, 1);
        RETURN;
    END;

    UPDATE EMAIL.Profiles
    SET HostName = @hostName,
        Port = @port,
        EnableSsl = @enableSsl,
        DefaultCred = @useDefaultCredentials,
        UserName = @userName,
        Password = CASE WHEN @password IS NULL THEN Password ELSE @encrypted END,
        Domain = @domain,
        DefaultFrom = @defaultFrom,
        DefaultGroup = @defaultDisplayName,
        DeliveryMethod = 0,
        TimeOut = @timeout
    WHERE ProfileName = @profileName;

    IF @@ROWCOUNT = 0
        INSERT INTO EMAIL.Profiles (ProfileName, HostName, Port, EnableSsl, DefaultCred, UserName, Password, Domain, DefaultFrom, DefaultGroup, DeliveryMethod, TimeOut)
        VALUES (@profileName, @hostName, @port, @enableSsl, @useDefaultCredentials, @userName, @encrypted, @domain, @defaultFrom, @defaultDisplayName, 0, @timeout);
END;
GO
--------------------------------------------------------------------------------------
-- Version 1's credentials, encrypted with its published key, re-encrypted with the
-- certificate EmailProfiles
--------------------------------------------------------------------------------------
IF COL_LENGTH(N'EMAIL.Profiles', N'PasswordV1') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql N'CREATE PROCEDURE EMAIL.UpgradeVersion1Profiles
        AS EXTERNAL NAME [SimpleTalk.SQLCLR.SendMail].[SimpleTalk.SqlClr.SendMail.StoredProcedures].[UpgradeVersion1Profiles];';
    EXEC sys.sp_executesql N'EXEC EMAIL.UpgradeVersion1Profiles;';
    DROP PROCEDURE EMAIL.UpgradeVersion1Profiles;
    ALTER TABLE EMAIL.Profiles DROP COLUMN UserNameV1, PasswordV1;
END;
GO
IF OBJECT_ID(N'EMAIL.QueryToHtml') IS NULL
    PRINT N'EMAIL.QueryToHtml and EMAIL.ConCatHtml, for HTML bodies, are installed by SetupHtml.sql.';
GO
SET NOEXEC OFF;
GO
