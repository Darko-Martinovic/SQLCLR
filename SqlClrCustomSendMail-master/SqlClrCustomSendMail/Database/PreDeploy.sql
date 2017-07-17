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

USE MASTER
GO
--Copy snk from VS solution to path visible for your sql server instance
--Replace this path value, you password as you want
IF NOT EXISTS (SELECT * FROM sys.asymmetric_keys WHERE name = N'keySendMail')
BEGIN
	CREATE ASYMMETRIC KEY keySendMail
	FROM FILE = 'D:\VS2017_PROJECTS\SqlClrCustomSendMail\SqlClrCustomSendMail\keySqlClrCustomSendMail.snk'
	ENCRYPTION BY PASSWORD = '@Str0ngP@$$w0rd'
END
GO

