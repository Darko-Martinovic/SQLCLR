using System;
using System.Data.SqlTypes;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.SqlServer.Server;
using SqlClrCustomSendMail;
using System.IO;
using System.Data.SqlClient;

public partial class StoredProcedures
{

    
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void CLRSendMail([SqlFacet( MaxSize = 20)] SqlString profileName,
          SqlString mailTo,
          [SqlFacet(IsNullable = true, MaxSize = 255)]SqlString mailSubject,
          SqlString mailBody,
          [SqlFacet(IsNullable = true, MaxSize = 500)]SqlString fromAddress,
          [SqlFacet(IsNullable = true, MaxSize = 400)]SqlString displayName,
          [SqlFacet(IsNullable = true, MaxSize = 4000)]SqlString mailCc,
          [SqlFacet(IsNullable = true, MaxSize = 4000)]SqlString blindCopyRec,
          [SqlFacet(IsNullable = true, MaxSize = 4000)]SqlString replyAddress,
          [SqlFacet(IsNullable = true, MaxSize = 4000)]SqlString fileAttachments,
          [SqlFacet(IsNullable = true)]SqlBoolean requestReadReceipt,
          [SqlFacet(IsNullable = true)]SqlInt16 deliveryNotification,
          [SqlFacet(IsNullable = true)]SqlInt16 sensitivity,
          [SqlFacet(IsNullable = true)]SqlInt16 mailPriorty,
          [SqlFacet(IsNullable = true)]SqlBoolean bodyHtml,
          [SqlFacet(IsNullable = true, MaxSize = 20)]SqlString configName)
    {


        SysProfile mailClrClient;
        MailMessage eMail = null;
        SysConfig sc;
        var validAttachment = string.Empty;
        var pipe = SqlContext.Pipe;
        try
        {

            //Check parameters
            if (profileName.IsNull || profileName.Value.Trim() == string.Empty)
            {
                pipe?.Send("Parameter @profileName must be specified !");
                return;
            }
            if (mailTo.IsNull || mailTo.Value.Trim() == string.Empty)
            {
                pipe?.Send("Parametar @mailTo must be specified !");
                return;
            }
            if (mailBody.IsNull || mailBody.Value.Trim() == string.Empty)
            {
                pipe?.Send("Parametar @mailBody must be specified !");
                return;
            }
            var error = "";
            
            if (configName.IsNull || configName.Value.Trim() == string.Empty)
                sc = new SysConfig();
            else
            {
                sc = GetConfig(configName.Value, ref error);
                if (sc == null)
                {
                    LogEntry.LogItem($"Configuration is not defined !",  "Warning",
                        $"There is an error when trying to determine config : {error}",
                        $"Config name : {configName.Value}");
                    pipe?.Send($"Configuration is not defined !{error}" != "" ? $"There is an error when trying to determine config : {error}"
                        : "");
                    return;
                }
            }

            mailClrClient = GetClient(profileName.Value, ref error);
            if (mailClrClient == null)
            {
                LogEntry.LogItem("Profile is not defined !",  "Warning",
                    $"There is an error when trying to determine profile : {error}",
                    $"Profile name : {profileName.Value}");
                pipe?.Send($"Profile is not defined !{error}" != "" ? $"There is an error when trying to determine profile : {error}"
                    : "");
                return;
            }


            eMail = ConstructEmailMessage(
                        sc,
                        mailTo.Value,
                        mailSubject.IsNull ? "SQLCLR Server Message" : mailSubject.Value,
                        mailBody.IsNull ? string.Empty : mailBody.Value,
                        DetermineFromAddress(fromAddress.IsNull ? string.Empty : fromAddress.Value,
                            displayName.IsNull ? string.Empty : displayName.Value, mailClrClient),
                        mailCc.IsNull ? string.Empty : (string)mailCc.Value,
                        blindCopyRec.IsNull ? string.Empty : (string)blindCopyRec.Value,
                        replyAddress.IsNull ? string.Empty : (string)replyAddress.Value,
                        fileAttachments.IsNull ? string.Empty : (string)fileAttachments.Value,
                        ref validAttachment,
                        !requestReadReceipt.IsNull && (bool)requestReadReceipt.Value,
                        sensitivity.IsNull ? -1 : (int)sensitivity.Value,
                        deliveryNotification.IsNull
                            ? DeliveryNotificationOptions.None
                            : (DeliveryNotificationOptions) Enum.Parse(typeof(DeliveryNotificationOptions),
                                deliveryNotification.Value.ToString()),
                        mailPriorty.IsNull ? MailPriority.Normal : (MailPriority)Enum.Parse(typeof(MailPriority), mailPriorty.Value.ToString()),
                        !bodyHtml.IsNull && (bool)bodyHtml.Value);



            if (mailClrClient.Client.EnableSsl)
                ServicePointManager.ServerCertificateValidationCallback = (object s, X509Certificate certificate,
                    X509Chain chain, SslPolicyErrors sslPolicyErrors) => true;


            if (sc.SendAsync)
            {
                //mailClrClient.Client.SendCompleted += new
                //SendCompletedEventHandler(SendCompletedCallback);
                // The userState can be any object that allows your callback 
                // method to identify this send operation.
                // For this example, the userToken is a string constant.
                //string userState = "SQLCLR does not fire this event";
                //mailClrClient.Client.SendAsync(eMail, userState);

                mailClrClient.Client.SendAsync(eMail, null);
            }
            else
            { 
                mailClrClient.Client.Send(eMail);
                eMail.Dispose();
            }
            if (sc.NoPiping == false)
                pipe.Send("Sent successfully!");

            if (sc.LoggingLevel == ELoggingLevel.Maximum)
                LogEntry.LogItem("SQLCLR sent e-mail", "Information", "E-mail successfully sent!", eMail.HeaderInformation());

            if (sc.SaveEmails)
                EmailTracker.SaveEmail(eMail, mailClrClient.Name, sc.Name, validAttachment,sc.SaveAttachments);

            if (sc.SendAsync == false)
                eMail.Dispose();
        }
        catch (Exception ex)
        {
            //if (sc.noPiping == false)
                pipe.Send($"There is an error in sending e-mail : {ex.Message}\r\n{ex.InnerException}" == null ? "" : ex.InnerException.Message);

            LogEntry.LogItem("SQLCLR sent e-mail",  "Error", $"E-mail was not sent!", ex.Message + "\r\n" + ex.InnerException == null ? "" : ex.InnerException.Message);
        }
        finally
        {
            if (eMail != null)
                eMail = null;

            sc = null;
            mailClrClient = null;
        }
        GC.Collect();
        GC.WaitForPendingFinalizers();



    }

    //static bool mailSent = false;
    //private static void SendCompletedCallback(object sender, AsyncCompletedEventArgs e)
    //{
    //    // Get the unique identifier for this asynchronous operation.
    //    String token = (string)e.UserState;

    //    if (e.Cancelled)
    //    {
    //    }
    //    if (e.Error != null)
    //    {
    //    }
    //    else
    //    {
    //    }
    //    mailSent = true;
    //}
    private static MailAddress DetermineFromAddress(string fromAddress, string displayName, SysProfile sp)
    {
        MailAddress retValue;
        if (fromAddress.Equals(string.Empty) == false)
        {
            retValue = displayName.Trim() != string.Empty
                ? new MailAddress(fromAddress, displayName)
                : new MailAddress(fromAddress, sp.DefaultDisplayName ?? "Simple Talk's CLR solution");
        }
        else
        {
            retValue = displayName.Trim() != string.Empty
                ? new MailAddress(sp.DefaultFromAddress, displayName)
                : new MailAddress(sp.DefaultFromAddress, sp.DefaultDisplayName ?? "Simple Talk's CLR solution");
        }

        return retValue;
    }

    private static MailMessage ConstructEmailMessage(
                    SysConfig sc,
                    string mailTo, 
                    string mailSubject, 
                    string mailBody, 
                    MailAddress fromAddress, 
                    string mailCc, 
                    string blindCopyRec, 
                    string replyAddress,
                    string fileAttachments,
                    ref string validAttachment,
                    bool requestReadReceipt,
                    int sensitivity,
                    DeliveryNotificationOptions deliveryNotification,
                    MailPriority mailPriorty,
                    bool bodyHtml)
    {
        var eMail = new MailMessage {From = fromAddress};


        if (replyAddress.Trim() != string.Empty)
        {
            foreach (var eml in replyAddress.Split(';'))
            {
                if (eml.Trim().Length > 0)
                {
                    eMail.ReplyToList.Add(eml.Trim());
                }
            }
        }

        foreach (var eml in mailTo.Split(';'))
        {
            if (eml.Trim().Length > 0)
                eMail.To.Add(eml.Trim());
        }

        if (mailCc.Trim() != string.Empty)
        {
            foreach (var eml in mailCc.Split(';'))
            {
                if (eml.Trim().Length > 0)
                    eMail.CC.Add(eml.Trim());
            }
        }

        if (blindCopyRec.Trim() != string.Empty)
        {
            foreach (var eml in blindCopyRec.Split(';'))
            {
                if (eml.Trim().Length > 0)
                    eMail.Bcc.Add(eml.Trim());
            }
        }
        if (fileAttachments.Trim() != string.Empty)
        {
            //message.Attachments.Add(new Attachment(PathToAttachment));
            foreach (string eml in fileAttachments.Split(';'))
            {
                if (eml.Trim().Length > 0)
                {
                    string ext = Path.GetExtension(eml).Replace(".", "");
                    if (sc.ProhibitedExtension.Contains(ext))
                    {
                        LogEntry.LogItem("Prohibit extension!",  "Warning",
                            $"Attachment : {eml} has prohibit extension!", eMail.HeaderInformation());
                        continue;
                    }
                    if (File.Exists(eml) == false)
                    {
                        LogEntry.LogItem("File does not exists",  "Warning",
                            $"Attachment :{eml} does not exists on file system!", eMail.HeaderInformation());
                        continue;
                    }
                    if (sc.MaxFileSize < new FileInfo(eml).Length)
                    {
                        LogEntry.LogItem("Max.file size limit!",  "Warning",
                            $"Attachment : {eml} has size larger then allowed!", eMail.HeaderInformation());
                        continue;
                    }

                    var a = new Attachment(eml) {NameEncoding = Encoding.UTF8};
                    eMail.Attachments.Add(a);
                    validAttachment += eml + ";";
                }

            }

        }

        if (requestReadReceipt)
            eMail.Headers.Add("Disposition-Notification-To",replyAddress == string.Empty ? eMail.From.Address : replyAddress);


        if (sensitivity >= 0 && sensitivity <= 2)
        {
            //sensitivity = "Personal" / "Private" / "Company-Confidential"
            if (sensitivity == 0)
                eMail.Headers.Add("Sensitivity", "Personal");
            else if (sensitivity == 1)
                eMail.Headers.Add("Sensitivity", "Private");
            else
                eMail.Headers.Add("Sensitivity", "Company-Confidential");
        }

        eMail.DeliveryNotificationOptions = deliveryNotification;
        eMail.Priority = mailPriorty;
        eMail.IsBodyHtml = bodyHtml;
       
        eMail.SubjectEncoding = Encoding.UTF8;
        eMail.BodyEncoding = Encoding.UTF8;

        eMail.Subject = mailSubject;
        eMail.Body = mailBody;

        return eMail;
    }

    private static SysConfig GetConfig(string name,ref string error)
    {
        var pName = new SqlParameter("name", name)
        {
            Size = 20,
            SqlDbType = System.Data.SqlDbType.Char
        };
        var listOfParams = new SqlParameter[1];
        listOfParams[0] = pName;
        var s = DataAccess.GetConfig(listOfParams, ref error);

        return s;
    }

    private static SysProfile GetClient(string name, ref string error)
    {
        SysProfile p;
        var wrapper = new EncryptSupport.Simple3Des(SecretWord);
        //Built in profile called ssl
        if (name == "ssl")
        {
            p = new SysProfile
            {
                BuilInName = name,
                Client = new SmtpClient
                {
                    UseDefaultCredentials = false,
                    Port = 587,
                    Host = "smtp.gmail.com",
                    EnableSsl = true,
                    Credentials = new NetworkCredential(wrapper.DecryptData("DE5ZET4hY95fZ7JadaxKqchFuvrR3p12vlY="),
                        wrapper.DecryptData("ovkrtZ/="))
                }
            };
            // encrypted credential 
        }
        //Built in profile called simple
        else if (name == "simple")
        {
            p = new SysProfile
            {
                BuilInName = name,
                Client = new SmtpClient
                {
                    UseDefaultCredentials = true,
                    Port = 25,
                    Host = "mail.iskon.hr",
                    EnableSsl = false
                }
            };
            // encrypted credential 
            //p.client.Credentials = new NetworkCredential(wrapper.DecryptData("El+=="), wrapper.DecryptData("=="));
        }
        else
        {
            //determine profile by quering the database
            var listOfParams = new SqlParameter[1];
            var pName = new SqlParameter("name", name)
            {
                Size = 20,
                SqlDbType = System.Data.SqlDbType.Char
            };
            listOfParams[0] = pName;
            p = DataAccess.GetProfile(listOfParams, wrapper,ref error);
        }
        return p;
    }

    public static readonly string SecretWord = "SimpleTalk";
}
