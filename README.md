# SQLCLR
The code written in any .NET language to be incorporated into your SQL Server instance and called from a stored procedure or function
#######
If you do not want to do anything with the source code, 
copy the t-sql script from 'Setup' directory to the SSMS. 
In that script, replace the phrase 'your database name' with the real name of your database. 
Similar do with 'DataPath' and 'LogPath'. 
This script also fills out the table of profiles. 
Use the encryption program described in the article to fill the profile table.
#####

--To send e-mail


EXEC [EMAIL].[CLRSendMail] @profileName = N'SimpleTalk'
						  ,@mailTo = N'yourEmail@Email.com'
						  ,@mailSubject = N'First test'
						  ,@mailBody = N'Mail body';
						  
						  
--To include query result in e-mail body


DECLARE @body as nvarchar(max)
SET @body = EMAIL.QueryToHtml('SELECT * FROM EMAIL.PROFILES', '', 'EMAIL.Profiles', '#', 2, 0, 'ST_BLUE')
EXEC [EMAIL].[CLRSendMail] @profileName = N'SimpleTalk'
						  ,@mailTo = N'yourEmail@Email.com'
						  ,@mailSubject = N'Test QueryToHtml'
						  ,@mailBody = @body;
						  
						  
--To include multiple query results 


SET @body = (SELECT
		EMAIL.ConCatHtml(@body, (SELECT
				EMAIL.QueryToHtml('SELECT
 *
FROM EMAIL.Configurations','',
				'EMAIL.Configuration', '#', 2, 0, 'ST_RED'))
		));

EXEC [EMAIL].[CLRSendMail] @profileName = N'SimpleTalk'
						  ,@mailTo = N'yourEmail@Email.com'
						  ,@mailSubject = N'Test ConCatHtml'
						  ,@mailBody = @body;
