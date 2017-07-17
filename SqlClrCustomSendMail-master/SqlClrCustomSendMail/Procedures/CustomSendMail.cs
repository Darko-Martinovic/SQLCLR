using System;
using System.Data.SqlTypes;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.SqlServer.Server;
using SqlClrCustomSendMail;
using System.ComponentModel;
using System.IO;
using System.Collections.Generic;
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


        SysProfile MailCLRClient = null;
        MailMessage eMail = null;
        SysConfig sc = null;
        string validAttachment = string.Empty;
        try
        {

            //Check parameters
            if (profileName.IsNull || profileName.Value.Trim() == string.Empty)
            {
                SqlContext.Pipe.Send("Parameter @profileName must be specified !");
                return;
            }
            if (mailTo.IsNull || mailTo.Value.Trim() == string.Empty)
            {
                SqlContext.Pipe.Send("Parametar @mailTo must be specified !");
                return;
            }
            if (mailBody.IsNull || mailBody.Value.Trim() == string.Empty)
            {
                SqlContext.Pipe.Send("Parametar @mailBody must be specified !");
                return;
            }
            string error = "";
            
            if (configName.IsNull || configName.Value.Trim() == string.Empty)
                sc = new SysConfig();
            else
            {
                sc = GetConfig(configName.Value, ref error);
                if (sc == null)
                {
                    LogEntry.LogItem("Configuration is not defined !",  "Warning",  "There is an error when trying to determine config : " + error, "Config name : " + configName.Value);
                    SqlContext.Pipe.Send("Configuration is not defined !" + error != "" ? "There is an error when trying to determine config : " + error : "");
                    return;
                }
            }

            MailCLRClient = GetClient(profileName.Value, ref error);
            if (MailCLRClient == null)
            {
                LogEntry.LogItem("Profile is not defined !",  "Warning", "There is an error when trying to determine profile : " + error, "Profile name : " + profileName.Value);
                SqlContext.Pipe.Send("Profile is not defined !" + error != "" ? "There is an error when trying to determine profile : " + error : "");
                return;
            }


            eMail = ConstructEmailMessage(
                        sc,
                        mailTo.Value.ToString(),
                        mailSubject.IsNull ? "SQLCLR Server Message" : mailSubject.Value.ToString(),
                        mailBody.IsNull ? String.Empty : mailBody.Value.ToString(),
                        DetermineFromAddress(fromAddress.IsNull ? string.Empty : fromAddress.Value.ToString(), displayName.IsNull ? string.Empty : displayName.Value.ToString(), MailCLRClient),
                        mailCc.IsNull ? string.Empty : (string)mailCc.Value,
                        blindCopyRec.IsNull ? string.Empty : (string)blindCopyRec.Value,
                        replyAddress.IsNull ? string.Empty : (string)replyAddress.Value,
                        fileAttachments.IsNull ? string.Empty : (string)fileAttachments.Value,
                        ref validAttachment,
                        requestReadReceipt.IsNull ? false : (bool)requestReadReceipt.Value,
                        sensitivity.IsNull ? -1 : (int)sensitivity.Value,
                        deliveryNotification.IsNull ? DeliveryNotificationOptions.None : (DeliveryNotificationOptions)Enum.Parse(typeof(DeliveryNotificationOptions), deliveryNotification.Value.ToString()),
                        mailPriorty.IsNull ? MailPriority.Normal : (MailPriority)Enum.Parse(typeof(MailPriority), mailPriorty.Value.ToString()),
                        bodyHtml.IsNull ? false : (bool)bodyHtml.Value);



            if (MailCLRClient.client.EnableSsl)
                ServicePointManager.ServerCertificateValidationCallback = (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true;


            if (sc.sendAsync)
                MailCLRClient.client.SendAsync(eMail, null);
            else
                MailCLRClient.client.Send(eMail);

            if (sc.noPiping == false)
                SqlContext.Pipe.Send("Sent successfully!");

            if (sc.loggingLevel == eLoggingLevel.Maximum)
                LogEntry.LogItem("SQLCLR sent e-mail", "Information", "E-mail successfully sent!", eMail.HeaderInformation());

            if (sc.saveEmails)
                EmailTracker.SaveEmail(eMail, MailCLRClient.name == null ? null : MailCLRClient.name, sc.name == null ? null : sc.name, validAttachment,sc.saveAttachments);

        }
        catch (Exception ex)
        {
            //if (sc.noPiping == false)
                SqlContext.Pipe.Send("There is an error in sending e-mail : " + ex.Message + "\r\n" + ex.InnerException == null ? "" : ex.InnerException.Message);

            LogEntry.LogItem("SQLCLR sent e-mail",  "Error",  "E-mail was not sent!", ex.Message + "\r\n" + ex.InnerException == null ? "" : ex.InnerException.Message);
        }
        finally
        {
            eMail = null;
            sc = null;
            MailCLRClient = null;
        }
        GC.Collect();
        GC.WaitForPendingFinalizers();



    }

    private static MailAddress DetermineFromAddress(string fromAddress, string displayName, SysProfile sp)
    {
        MailAddress retValue = null;
        if (fromAddress.Equals(string.Empty) == false)
        {
            if (displayName.Trim() != string.Empty)
                retValue = new MailAddress(fromAddress, displayName);
            else
                retValue = new MailAddress(fromAddress, sp.defaultDisplayName == null ? "DEFAULT GROUP" : sp.defaultDisplayName);
        }
        else
        {
            if (displayName.Trim() != String.Empty)
                retValue = new MailAddress(sp.defaultFromAddress, displayName);
            else
                retValue = new MailAddress(sp.defaultFromAddress, sp.defaultDisplayName == null ? "DEFAULT GROUP" : sp.defaultDisplayName);

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
        MailMessage eMail = new MailMessage();
        eMail.From = fromAddress;
        

        if (replyAddress.Trim() != string.Empty)
        {
            foreach (string eml in replyAddress.Split(';'))
            {
                if (eml.Trim().Length > 0)
                {
                    eMail.ReplyToList.Add(eml.Trim());
                }
            }
        }

        foreach (string eml in mailTo.Split(';'))
        {
            if (eml.Trim().Length > 0)
                eMail.To.Add(eml.Trim());
        }

        if (mailCc.Trim() != string.Empty)
        {
            foreach (string eml in mailCc.ToString().Split(';'))
            {
                if (eml.Trim().Length > 0)
                    eMail.CC.Add(eml.Trim());
            }
        }

        if (blindCopyRec.Trim() != string.Empty)
        {
            foreach (string eml in blindCopyRec.ToString().Split(';'))
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
                    if (sc.prohibitedExtension.Contains(ext))
                    {
                        LogEntry.LogItem("Prohibit extension!",  "Warning",  "Attachment : " + eml + " has prohibit extension!", eMail.HeaderInformation());
                        continue;
                    }
                    if (File.Exists(eml) == false)
                    {
                        LogEntry.LogItem("File does not exists",  "Warning",  "Attachment :" + eml + " does not exists on file system!", eMail.HeaderInformation());
                        continue;
                    }
                    if (sc.maxFileSize < new FileInfo(eml).Length)
                    {
                        LogEntry.LogItem("Max.file size limit!",  "Warning",  "Attachment : " + eml + " has size larger then allowed!", eMail.HeaderInformation());
                        continue;
                    }
                    Attachment a = new Attachment(eml);
                    a.NameEncoding = Encoding.UTF8;
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
        SysConfig s = null;

        SqlParameter pName = new SqlParameter("name", name);
        pName.Size = 20;
        pName.SqlDbType = System.Data.SqlDbType.Char;
        SqlParameter[] listOfParams = new SqlParameter[1];
        listOfParams[0] = pName;
        s = DataAccess.GetConfig(listOfParams, ref error);

        return s;
    }

    private static SysProfile GetClient(string name, ref string error)
    {
        SysProfile p = null;
        EncryptSupport.Simple3Des wrapper = new EncryptSupport.Simple3Des(SECRET_WORD);
        //Built in profile called ssl
        if (name == "ssl")
        {
            p = new SysProfile();
            p.builInName = name;
            p.client = new SmtpClient();
            p.client.UseDefaultCredentials = false;
            p.client.Port = 587;
            p.client.Host = "smtp.gmail.com";
            p.client.EnableSsl= true;
            // encrypted credential 
            p.client.Credentials = new NetworkCredential(wrapper.DecryptData("DE5ZET4hY95fZ7JadaxKqchFuvrR3p12vlY="), wrapper.DecryptData("ovkrtZ/="));
        }
        //Built in profile called simple
        else if (name == "simple")
        {
            p = new SysProfile();
            p.builInName = name;
            p.client = new SmtpClient();
            p.client.UseDefaultCredentials = false;
            p.client.Port = 25;
            p.client.Host = "mail.iskon.hr";
            p.client.EnableSsl = false;
            // encrypted credential 
            p.client.Credentials = new NetworkCredential(wrapper.DecryptData("El+=="), wrapper.DecryptData("=="));
        }
        else
        {
            //determine profile by quering the database
            SqlParameter[] listOfParams = new SqlParameter[1];
            SqlParameter pName = new SqlParameter("name", name);
            pName.Size = 20;
            pName.SqlDbType = System.Data.SqlDbType.Char;
            listOfParams[0] = pName;
            p = DataAccess.GetProfile(listOfParams, wrapper,ref error);
        }
        return p;
    }

    public static readonly string SECRET_WORD = "SimpleTalk";
}
