--Drop stored procedure EMAIL.CLRSendMail
IF EXISTS ( SELECT  *
			FROM    sys.objects
			WHERE   object_id = OBJECT_ID(N'[EMAIL].[CLRSendMail]')
			   AND OBJECTPROPERTY(object_id, N'IsProcedure') = 1 )
BEGIN
	DROP PROCEDURE [EMAIL].[CLRSendMail]
END

--Drop help function 
IF EXISTS ( SELECT * 
			FROM   sysobjects 
			WHERE  id = object_id(N'[EMAIL].[CustomSendMailHelp]') 
				   and type = N'FT' )
BEGIN
	DROP FUNCTION [EMAIL].[CustomSendMailHelp]
END

--Drop assembly 
IF  EXISTS ( SELECT * 
			 FROM sys.assemblies asms 
			 WHERE asms.name = N'SimpleTalk.SQLCLR.SendMail' 
			 and is_user_defined = 1)
BEGIN
	DROP ASSEMBLY [SimpleTalk.SQLCLR.SendMail]
END
GO

-- Drop attachments
IF  EXISTS ( SELECT * 
			 FROM sys.objects
			 WHERE object_id = OBJECT_ID(N'[Email].[MailAttachments]')
			 AND sys.objects.type = 'U')
BEGIN
	DROP TABLE [EMAIL].[MailAttachments]
END
GO

-- Drop mail items
IF  EXISTS ( SELECT * 
			 FROM sys.objects
			 WHERE object_id = OBJECT_ID(N'[Email].[MailItems]')
			 AND sys.objects.type = 'U')
BEGIN
	DROP TABLE [EMAIL].[MailItems]
END
GO

-- Drop configuration [Email].[Configurations]
IF  EXISTS ( SELECT * 
			 FROM sys.objects
			 WHERE object_id = OBJECT_ID(N'[Email].[Configurations]')
			 AND sys.objects.type = 'U')
BEGIN
	DROP TABLE [Email].[Configurations]
END
GO

-- Drop profiles
IF  EXISTS ( SELECT * 
			 FROM sys.objects
			 WHERE object_id = OBJECT_ID(N'[Email].[Profiles]')
			 AND sys.objects.type = 'U')
BEGIN
	DROP TABLE [Email].[Profiles]
END
GO

-- Drop Monitor log
IF  EXISTS ( SELECT * 
			 FROM sys.objects
			 WHERE object_id = OBJECT_ID(N'[Email].[MonitorLog]')
			 AND sys.objects.type = 'U')
BEGIN
	DROP TABLE [Email].[MonitorLog]
END
GO
-- Drop type 
IF EXISTS (SELECT *
			   FROM sys.types st
			   JOIN sys.schemas ss
				  ON st.schema_id = ss.schema_id
			   WHERE st.name = N'TVP_Emails'
				  AND ss.name = N'EMail')
BEGIN
   DROP TYPE [EMail].[TVP_EMAILS]
END

GO
--Drop user
IF EXISTS (SELECT name
	FROM sys.database_principals
	WHERE name = 'UserSqlClrCustomSendMail')
BEGIN
	DROP USER UserSqlClrCustomSendMail 
END
GO
--Drop schema
IF EXISTS (
	SELECT schema_name
	FROM information_schema.schemata
	WHERE schema_name = 'EMAIL' )

BEGIN
	EXEC sp_executesql N'DROP SCHEMA EMAIL'
END
GO

IF EXISTS (SELECT loginname
	FROM master.dbo.syslogins
	WHERE name = 'SqlClrCustomSendMail')
BEGIN
	DECLARE @sqlStatement AS NVARCHAR(1000)
	SELECT @SqlStatement = 'DROP LOGIN [SqlClrCustomSendMail] ';  
	EXEC sp_executesql @SqlStatement
END
GO 

--Drop KEY
IF EXISTS (SELECT *
		   FROM master.sys.asymmetric_keys
		   WHERE name = 'keySendMail')
BEGIN
	DECLARE @sqlStatement AS NVARCHAR(1000)
	SELECT @SqlStatement = 'USE MASTER 
							DROP ASYMMETRIC KEY keySendMail
	';  
	EXEC sp_executesql @SqlStatement


	
END
GO

