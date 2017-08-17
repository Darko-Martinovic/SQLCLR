/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.		
 Use SQLCMD syntax to include a file in the post-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script.		
 Example:      :setvar TableName MyTable							
			   SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/
ALTER PROCEDURE [dbo].[CLRSendMail]
	@profileName NVARCHAR (MAX), 
	@mailTo NVARCHAR (MAX), 
	@mailSubject NVARCHAR (255)=N'SQLCLR Server Message', 
	@mailBody NVARCHAR (MAX), 
	@fromAddress NVARCHAR (500)=NULL,
	@displayName NVARCHAR (400)=NULL, 
	@mailCc NVARCHAR (4000)=NULL,
	@blindCopyRec NVARCHAR (4000)=NULL, 
	@replyAddress NVARCHAR (4000)=NULL, 
	@fileAttachments NVARCHAR (4000)=NULL, 
	@requestReadReceipt BIT=0, 
	@deliveryNotification SMALLINT=0, 
	@sensitivity SMALLINT=-1, 
	@mailPriorty SMALLINT=0, 
	@bodyHtml BIT=1, 
	@configName NVARCHAR(20)=NULL
AS EXTERNAL NAME [SimpleTalk.SQLCLR.SendMail].[StoredProcedures].[CLRSendMail];

GO

--------------------------------------------------------------------------------------------------------------------------------------------------------
----------------------------------------------------------------Add default record in profile table
--------------------------------------------------------------------------------------------------------------------------------------------------------
-- REPLACE YOUR LOGIN NAME WITH encrypted value of your login name
--E.g. If your login name is TestAccount@gmail.com and the secret work 'SimpleTalk' then replace N'your login name' WITH N'BsqZkKED9s2XT/1mRt4ci2at9lb4ewGAN1I0RWiEYgzEl39KIo/ONgpM0FqOI3KC'
--Do the same for password

IF ( SELECT COUNT(*) FROM EMAIL.PROFILES ) = 0 
BEGIN
INSERT INTO email.profiles (profilename, EnableSsl, DefaultCred, Port, HostName, UserName, Password,DefaultFrom)
		SELECT
			'SimpleTalk'
			,1
			,0
			,587
			,'smtp.gmail.com'
			,N'your login name'
			,N'your password'
			,'your default from address';

---Use these values if you decide to apply T-SQL encryption by using ENCRYPTBYCERT function 
			--,ENCRYPTBYCERT(CERT_ID('TestCert'), N'your login name')
			--,ENCRYPTBYCERT(CERT_ID('TestCert'), N'your password')
			--,'darko.martinovic@outlook.com';
	

END



--------------------------------------------------------------------------------------------------------------------------------------------------------
----------------------------------------------------------------Add default record in configuration table
--------------------------------------------------------------------------------------------------------------------------------------------------------
IF ( SELECT COUNT(*) FROM [Email].[Configurations] ) = 0 
BEGIN
INSERT INTO [Email].[Configurations] (name)
		SELECT
			'default';
END


--------------------------------------------------------------------------------------------------------------------------------------------------------
----------------------------------------------------------------Transfer to email schema
--------------------------------------------------------------------------------------------------------------------------------------------------------
IF EXISTS ( SELECT * 
			FROM   sysobjects 
			WHERE  id = object_id(N'[EMAIL].[CLRSendMail]') 
				   and OBJECTPROPERTY(id, N'IsProcedure') = 1 )
BEGIN
	DROP PROCEDURE [EMAIL].[CLRSendMail]
END

ALTER SCHEMA EMAIL TRANSFER dbo.CLRSendMail;

--Transfer function
IF EXISTS ( SELECT * 
			FROM   sysobjects 
			WHERE  id = object_id(N'[EMAIL].[CustomSendMailHelp]') 
				   and type = N'FT' )
BEGIN
	DROP FUNCTION [EMAIL].[CustomSendMailHelp]
END

ALTER SCHEMA EMAIL TRANSFER dbo.CustomSendMailHelp;



--Transfer function
IF EXISTS (SELECT
		*
	FROM sysobjects
	WHERE id = OBJECT_ID(N'[EMAIL].[CleanMemory]')
	AND type = N'FS')
BEGIN
DROP FUNCTION [EMAIL].[CleanMemory]
END

ALTER SCHEMA EMAIL TRANSFER dbo.CleanMemory;



--Transfer function QueryToHtml
IF EXISTS (SELECT
		*
	FROM sysobjects
	WHERE id = OBJECT_ID(N'[EMAIL].[QueryToHtml]')
	AND type = N'FS')
BEGIN
DROP FUNCTION [EMAIL].[QueryToHtml]
END

ALTER SCHEMA EMAIL TRANSFER dbo.QueryToHtml;




--------------------------------------------------------------------------------------------------------------------------------------------------------
----------------------------------------------------------------Alter authorization
--------------------------------------------------------------------------------------------------------------------------------------------------------
ALTER AUTHORIZATION ON ASSEMBLY::[SimpleTalk.SQLCLR.SendMail] TO [UserSqlClrCustomSendMail]