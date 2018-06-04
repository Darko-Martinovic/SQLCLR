--Test UNC path
EXEC [EMAIL].[CLRSendMail] @profileName = N'SimpleTalk'
					 ,@mailTo = N'darko.martinovic@outlook.com'
					 ,@configName = N'sync' -- send sync a message is disposed after sending
					 ,@mailSubject = 'test'
					 ,@fileAttachments = N'\\db2\Restore\b.docx'
					 ,@mailBody = N'body mail ';
--Test attachments
EXEC [EMAIL].[CLRSendMail]	@profileName = N'SimpleTalk'
							,@mailTo = N'darko.martinovic@outlook.com'
							,@configName =N'default'
							,@mailSubject = 'Test attachments'
							,@fileAttachments = 'D:\a.docx'
							,@mailBody = N'Test attachments';

--Test query to html
DECLARE @body as nvarchar(max)
SET @body = EMAIL.QueryToHtml('SELECT * FROM sys.databases', '', 'sys.databases', '#', 2, 0, 'StRed')
EXEC [EMAIL].[CLRSendMail] @profileName = N'SimpleTalk'
						  ,@mailTo = N'darko.martinovic@outlook.com'
						  ,@mailSubject = N'Test QueryToHtml'
						  ,@mailBody = @body;


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
