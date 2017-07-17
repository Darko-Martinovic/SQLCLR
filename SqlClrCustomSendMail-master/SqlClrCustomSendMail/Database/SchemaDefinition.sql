
--------------------------------------------------------------------------------------------------------------------------------------------------------
----------------------------------------------------------------Create schema
--------------------------------------------------------------------------------------------------------------------------------------------------------
IF NOT EXISTS (
	SELECT schema_name
	FROM information_schema.schemata
	WHERE schema_name = 'EMAIL' )

BEGIN
	EXEC sp_executesql N'CREATE SCHEMA EMAIL'
END

--------------------------------------------------------------------------------------------------------------------------------------------------------
----------------------------------------------------------------Create table profiles
--------------------------------------------------------------------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT *
	FROM sys.objects
	WHERE object_id = OBJECT_ID(N'[Email].[Profiles]')
		AND type IN (N'U'))
BEGIN
	CREATE TABLE [Email].[Profiles] ( [ProfileName ]  [char](20)         NOT NULL 
	,                                 [EnableSsl]     [bit]              NOT NULL DEFAULT 1
	,                                 [DefaultCred]   [bit]              NOT NULL DEFAULT 0
	,                                 [HostName]      [nvarchar](50)     NOT NULL DEFAULT 'smtp.gmail.com'
	,                                 [Port]          [int]              NOT NULL DEFAULT 587
	,                                 [UserName]      [nvarchar](200)    NOT NULL 
	,                                 [Password]      [nvarchar](200)    NOT NULL 
	,                                 [DOMAIN]        [nvarchar](200)    NULL                     --Exchange domain name
	,                                 [DefaultFrom]   [nvarchar](500)    NOT NULL                 --Default from address
	,                                 [DefaultGroup]  [nvarchar](100)    NULL                     --Default displayName 
	,                                 [DeliveryMethod][tinyint]          NOT NULL DEFAULT 0
	,                                 [TimeOut]       [int]              NOT NULL DEFAULT 100000   
	,                                 CONSTRAINT [PK_Email_Profile_ProfileName] PRIMARY KEY CLUSTERED
	(
	[ProfileName] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY] ) ON [PRIMARY]
	--DROP TABLE [Email].[Profiles];
END
GO
------------------------------------Use these columns definition if you decide to apply T-SQL encryption 
	--,                                 [UserName]      [varbinary](128)  NOT NULL 
	--,                                 [Password]      [varbinary](128)  NOT NULL 
	--,                                 [DOMAIN]        [varbinary](128)  NULL                     --Exchange domain name

--------------------------------------------------------------------------------------------------------------------------------------------------------
----------------------------------------------------------------Create table configurations
--------------------------------------------------------------------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT *
	FROM sys.objects
	WHERE object_id = OBJECT_ID(N'[Email].[Configurations]')
		AND type IN (N'U'))
BEGIN
	CREATE TABLE [Email].[Configurations] ( [Name]                      [char](20) NOT NULL 
	,                                      [MaxFileSize]               [int]           NOT NULL DEFAULT 1000000
	,                                      [ProhibitedExtensions]      [nvarchar](50)  NOT NULL DEFAULT 'exe,dll,vbs,js'
	,                                      [LoggingLevel]              [tinyint]       NOT NULL DEFAULT 0
	,                                      [SaveEmails]                [bit]           NOT NULL DEFAULT 1
	,                                      [SendAsync]                 [bit]           NOT NULL DEFAULT 1
	,                                      [NoPiping]                  [bit]           NOT NULL DEFAULT 1
	,                                      [SaveAttachments]           [bit]           NOT NULL DEFAULT 1
	,                                      CONSTRAINT [PK_Email_Configurations_Name] PRIMARY KEY CLUSTERED
	(
	[Name] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY] ) ON [PRIMARY]
	--DROP TABLE [Email].[Configurations];
END
GO


--------------------------------------------------------------------------------------------------------------------------------------------------------
----------------------------------------------------------------Create table monitorlog
--------------------------------------------------------------------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT *
	FROM sys.objects
	WHERE object_id = OBJECT_ID(N'[Email].[MonitorLog]')
		AND type IN (N'U'))
BEGIN
	CREATE TABLE [Email].[MonitorLog]    ( [ID]                        [bigint]        IDENTITY(1,1) NOT NULL 
	,                                      [TimeStamp]                 [datetime]      NOT NULL DEFAULT(GETDATE())
	,                                      [Source]                    [nvarchar](50)  NOT NULL 
	,                                      [Type]                      [nvarchar](20)  NOT NULL 
	,                                      [Message]                   [nvarchar](100) NOT NULL 
	,                                      [Data]                      [nvarchar](MAX) NULL
	,                                      CONSTRAINT [PK_Email_MonitorLog_Id] PRIMARY KEY CLUSTERED
	(
	[ID] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY] ) ON [PRIMARY]
	--DROP TABLE [Email].[MonitorLog];
END
GO

--------------------------------------------------------------------------------------------------------------------------------------------------------
----------------------------------------------------------------Create table mail items
--------------------------------------------------------------------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT * 
	FROM sys.objects 
	WHERE object_id = OBJECT_ID(N'[Email].[MailItems]') 
		AND type in (N'U'))
BEGIN
	CREATE TABLE [Email].[MailItems](  [mailitem_id]              [bigint] IDENTITY(1,1) NOT NULL,
									   [profileName]              [char](20)  NULL,
									   [configurationName]        [char](20)  NULL,
									   [recipients]               [varchar](max) NULL,
									   [copy_recipients]          [varchar](max) NULL,
									   [blind_copy_recipients]    [varchar](max) NULL,
									   [subject]                  [nvarchar](255) NULL,
									   [from_address]             [varchar](max) NULL,
									   [reply_to]                 [varchar](max) NULL,
									   [body]                     [nvarchar](max) NULL,
									   [body_format]              [varchar](20) NULL,
									   [importance]               [varchar](6) NULL,
									   [sensitivity]              [varchar](50) NULL,
									   [file_attachments]         [nvarchar](max) NULL,
									   [last_mod_date]            [datetime] NOT NULL DEFAULT (GETDATE()),
									   [last_mod_user]               [sysname] NOT NULL DEFAULT (SUSER_SNAME()),
									   CONSTRAINT [PK_Email_MailItems_MailId] PRIMARY KEY CLUSTERED 
	(
	[mailitem_id] ASC
	) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY] ) ON [PRIMARY]
END
GO
--------------------------------------------------------------------------------------------------------------------------------------------------------
----------------------------------------------------------------Add foreign keys
--------------------------------------------------------------------------------------------------------------------------------------------------------

IF NOT EXISTS (SELECT
		*
	FROM sys.objects o
	WHERE o.object_id = OBJECT_ID(N'[EMAIL].[FK_MailItems_Profile]')
	AND OBJECTPROPERTY(o.object_id, N'IsForeignKey') = 1)
BEGIN
	ALTER TABLE [EMAIL].[MailItems] 
		WITH CHECK ADD CONSTRAINT [FK_MailItems_Profile] 
		FOREIGN KEY ([profileName]) 
		REFERENCES [EMAIL].[Profiles] ([ProfileName])
END

IF NOT EXISTS (SELECT
		*
	FROM sys.objects o
	WHERE o.object_id = OBJECT_ID(N'[EMAIL].[FK_MailItems_Configurations]')
	AND OBJECTPROPERTY(o.object_id, N'IsForeignKey') = 1)
BEGIN
	ALTER TABLE [EMAIL].[MailItems] 
		WITH CHECK ADD CONSTRAINT [FK_MailItems_Configurations] 
		FOREIGN KEY ([configurationName]) 
		REFERENCES [EMAIL].[Configurations] ([Name])
END

--------------------------------------------------------------------------------------------------------------------------------------------------------
----------------------------------------------------------------Create table mail attachments
--------------------------------------------------------------------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT * 
	 FROM sys.objects 
	 WHERE object_id = OBJECT_ID(N'[EMail].[MailAttachments]') 
		 AND type in (N'U'))
BEGIN
CREATE TABLE [EMail].[MailAttachments](
	[attachment_id]    [bigint] IDENTITY(1,1) NOT NULL,
	[mailitem_id]      [bigint] NOT NULL,
	[filename]         [nvarchar](512) NOT NULL,
	[filesize]         [bigint] NOT NULL,
	[attachment]       [varbinary](max) NULL,
	[last_mod_date]    [datetime] NOT NULL DEFAULT (GETDATE()),
	[last_mod_user]    [sysname] NOT NULL DEFAULT (SUSER_SNAME()),
	CONSTRAINT [PK_Email_MailAttachments_AttachmentId] PRIMARY KEY CLUSTERED 
	(
	[attachment_id] ASC
	) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY] ) 

END
GO
--------------------------------------------------------------------------------------------------------------------------------------------------------
----------------------------------------------------------------Add foreign keys
--------------------------------------------------------------------------------------------------------------------------------------------------------

IF NOT EXISTS (SELECT
		*
	FROM sys.objects o
	WHERE o.object_id = OBJECT_ID(N'[EMAIL].[FK_MailAttachments_MailItems]')
	AND OBJECTPROPERTY(o.object_id, N'IsForeignKey') = 1)
BEGIN
	ALTER TABLE [EMAIL].[MailAttachments] 
		WITH CHECK ADD CONSTRAINT [FK_MailAttachments_MailItems] 
		FOREIGN KEY ([mailitem_id]) 
		REFERENCES [EMAIL].[MailItems] ([mailitem_id])
END



--------------------------------------------------------------------------------------------------------------------------------------------------------
----------------------------------------------------------------Create type for attachments
--------------------------------------------------------------------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT
		*
	FROM sys.types st
	JOIN sys.schemas ss
		ON st.schema_id = ss.schema_id
	WHERE st.name = N'TVP_Emails'
	AND ss.name = N'EMail')
CREATE TYPE [EMail].[TVP_Emails] AS TABLE 
(
   [FileName]      [nvarchar](260)    NOT NULL,
   [FileSize]      [bigint]           NOT NULL,
   [Attachment]    [varbinary](max)   NOT NULL
)
GO


USE MASTER
GO
--Copy snk from VS solution to path visible for your sql server instance
--Replace this path value, you password as you want
IF NOT EXISTS (SELECT * FROM sys.asymmetric_keys WHERE name = N'keySendMail')
BEGIN
	CREATE ASYMMETRIC KEY keySendMail
	FROM FILE = $(keyPath)
	ENCRYPTION BY PASSWORD = '@Str0ngP@$$w0rd'
END
GO

--Use database where your installed the assembly
USE [$(DatabaseName)];
GO

--Create login if not exists
IF NOT EXISTS (SELECT loginname
	FROM master.dbo.syslogins
	WHERE name = 'SqlClrCustomSendMail')
BEGIN
	CREATE LOGIN SqlClrCustomSendMail FROM ASYMMETRIC KEY keySendMail
END
GO

USE MASTER
GO
--Grant rights to newly create login
GRANT UNSAFE ASSEMBLY TO SqlClrCustomSendMail;

USE [$(DatabaseName)];
GO 
--Create user 
IF NOT EXISTS (SELECT name
	FROM sys.database_principals
	WHERE name = 'UserSqlClrCustomSendMail')
BEGIN
	CREATE USER UserSqlClrCustomSendMail FOR LOGIN SqlClrCustomSendMail
END


