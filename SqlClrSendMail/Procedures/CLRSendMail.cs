using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Net.Mail;
using Microsoft.SqlServer.Server;

namespace SimpleTalk.SqlClr.SendMail
{
    public partial class StoredProcedures
    {
        /// <summary>
        /// Sends an e-mail through the SMTP server of a profile in EMAIL.Profiles, while the caller
        /// waits: a message that can't be sent is an error, logged in EMAIL.MonitorLog.
        /// </summary>
        [SqlProcedure]
        public static void CLRSendMail(
            SqlString profileName,
            SqlString mailTo,
            SqlString mailSubject,
            SqlString mailBody,
            SqlString fromAddress,
            SqlString displayName,
            SqlString mailCc,
            SqlString blindCopyRec,
            SqlString replyAddress,
            SqlString fileAttachments,
            SqlBoolean requestReadReceipt,
            SqlInt16 deliveryNotification,
            SqlInt16 sensitivity,
            SqlInt16 mailPriorty,
            SqlBoolean bodyHtml,
            SqlString configName)
        {
            using (var cnn = new SqlConnection("context connection=true"))
            {
                cnn.Open();
                string? header = null;
                try
                {
                    var config = Value(configName) == null ? MailConfiguration.BuiltIn() : MailConfiguration.Load(cnn, Value(configName)!);
                    var profile = MailProfile.Load(cnn, Required(profileName, "@profileName"));
                    var request = new MailRequest
                    {
                        To = Required(mailTo, "@mailTo"),
                        Subject = Value(mailSubject) ?? "SQLCLR Server Message",
                        Body = Required(mailBody, "@mailBody"),
                        FromAddress = Value(fromAddress),
                        DisplayName = Value(displayName),
                        Cc = Value(mailCc),
                        Bcc = Value(blindCopyRec),
                        ReplyTo = Value(replyAddress),
                        FileAttachments = Value(fileAttachments),
                        RequestReadReceipt = !requestReadReceipt.IsNull && requestReadReceipt.Value,
                        DeliveryNotification = (DeliveryNotificationOptions)InRange(deliveryNotification, 0, 0, 7, "@deliveryNotification", "0 none, 1 on success, 2 on failure, 4 on delay, or their sum"),
                        Sensitivity = InRange(sensitivity, -1, -1, 2, "@sensitivity", "-1 none, 0 Personal, 1 Private, 2 Company-Confidential"),
                        Priority = (MailPriority)InRange(mailPriorty, 0, 0, 2, "@mailPriorty", "0 normal, 1 low, 2 high"),
                        BodyHtml = bodyHtml.IsNull || bodyHtml.Value
                    };

                    var attachments = MessageBuilder.ReadAttachments(request.FileAttachments, config);
                    using (var message = MessageBuilder.Build(request, profile, attachments))
                    using (var client = profile.CreateClient())
                    {
                        header = MailLog.Header(message);
                        MailProfile.RequireModernTls();
                        client.Send(message);

                        if (!config.NoPiping)
                            PipeUtilities.Print("Sent successfully!");

                        if (config.LoggingLevel == LoggingLevel.Maximum)
                            MailLog.Write(cnn, "Information", "E-mail successfully sent!", header);

                        if (config.SaveEmails)
                            Save(cnn, message, profile, config, attachments, header);
                    }
                }
                catch (SendMailException ex)
                {
                    Fail(cnn, ex.Message, header);
                }
                catch (Exception ex)
                {
                    Fail(cnn, $"The e-mail was not sent: {MailProfile.Describe(ex)}", header);
                }
            }
        }

        /// <summary>
        /// The message has gone, so a failure to keep it is a warning, not an error.
        /// </summary>
        private static void Save(SqlConnection cnn, MailMessage message, MailProfile profile, MailConfiguration config,
            List<FileAttachment> attachments, string header)
        {
            try
            {
                MailLog.SaveEmail(cnn, message, profile.Name, config.Name, attachments, config.SaveAttachments);
            }
            catch (SqlException ex)
            {
                PipeUtilities.Print($"The e-mail was sent, but not saved in EMAIL.MailItems: {ex.Message}");
                TryLog(cnn, "Warning", "E-mail was sent, but not saved!", $"{ex.Message}\r\n{header}");
            }
        }

        private static void Fail(SqlConnection cnn, string message, string? header)
        {
            TryLog(cnn, "Error", "E-mail was not sent!", header == null ? message : $"{message}\r\n{header}");
            PipeUtilities.RaiseError(cnn, message);
        }

        private static void TryLog(SqlConnection cnn, string type, string message, string data)
        {
            try
            {
                MailLog.Write(cnn, type, message, data);
            }
            catch (SqlException)
            {
                // The log is a help, not a condition: the error itself still reaches the caller
            }
        }

        private static string? Value(SqlString value) =>
            value.IsNull || value.Value.Trim().Length == 0 ? null : value.Value.Trim();

        private static string Required(SqlString value, string name) =>
            Value(value) ?? throw new SendMailException($"Parameter {name} must be specified!");

        private static int InRange(SqlInt16 value, int whenNull, int min, int max, string name, string meaning)
        {
            var number = value.IsNull ? whenNull : value.Value;
            if (number < min || number > max)
                throw new SendMailException($"{name} is {number}: use {meaning}.");
            return number;
        }
    }

    /// <summary>A reason not to send, reported as it is.</summary>
    [Serializable]
    public sealed class SendMailException : Exception
    {
        public SendMailException(string message) : base(message)
        {
        }
    }
}
