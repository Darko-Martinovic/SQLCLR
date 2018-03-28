exec [EMAIL].[CLRSendMail] @profileName = N'simple'
								  ,@mailTo = N'darko.martinovic@outlook.com'
								  ,@mailSubject = 'Test annonymous'
								  ,@mailBody = 'Body contents'
								  ,@fromAddress = 'pomocniracun27@gmial.com'

--Test sending e-mail
EXEC [EMAIL].[CLRSendMail] @profileName = N'SimpleTalk'
						  ,@mailTo = N'darko.martinovic@outlook.com'
						  ,@mailSubject = N'Slanje putem outlooka'
						  ,@mailBody = N'Outlook';
--Test query to html
DECLARE @body as nvarchar(max)
SET @body = EMAIL.QueryToHtml('SELECT * FROM EMAIL.PROFILES', '', 'EMAIL.Profiles', '#', 2, 0, 'ST_SIMPLE')
EXEC [EMAIL].[CLRSendMail] @profileName = N'SimpleTalk'
						  ,@mailTo = N'darko.martinovic@outlook.com'
						  ,@mailSubject = N'Test QueryToHtml'
						  ,@mailBody = @body;
--Test ConCat Html
SET @body = (SELECT
		EMAIL.ConCatHtml(@body, (SELECT
				EMAIL.QueryToHtml('SELECT
 *
FROM EMAIL.Configurations','',
				'EMAIL.Configuration', '#', 2, 0, 'ST_RED'))
		));

EXEC [EMAIL].[CLRSendMail] @profileName = N'SimpleTalk'
						  ,@mailTo = N'darko.martinovic@outlook.com'
						  ,@mailSubject = N'Test QueryToHtml'
						  ,@mailBody = @body;
