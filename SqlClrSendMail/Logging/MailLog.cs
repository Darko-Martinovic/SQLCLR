using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Net.Mail;
using System.Text;

namespace SimpleTalk.SqlClr.SendMail
{
    /// <summary>
    /// EMAIL.MonitorLog, and the sent messages in EMAIL.MailItems and EMAIL.MailAttachments.
    /// </summary>
    internal static class MailLog
    {
        public static void Write(SqlConnection cnn, string type, string message, string? data)
        {
            using (var command = new SqlCommand(
                "INSERT INTO EMAIL.MonitorLog (Source, Type, Message, Data) VALUES (@source, @type, @message, @data);", cnn))
            {
                command.Parameters.Add("@source", SqlDbType.NVarChar, 50).Value = "SQLCLR sent e-mail";
                command.Parameters.Add("@type", SqlDbType.NVarChar, 20).Value = type;
                command.Parameters.Add("@message", SqlDbType.NVarChar, 100).Value = Cut(message, 100);
                command.Parameters.Add("@data", SqlDbType.NVarChar, -1).Value = (object?)data ?? DBNull.Value;
                command.ExecuteNonQuery();
            }
        }

        public static string Header(MailMessage message) =>
            $"From     : '{message.From.Address}'\r\nTo       : '{List(message.To)}'\r\nSubject  : '{message.Subject}'";

        public static void SaveEmail(SqlConnection cnn, MailMessage message, string profileName, string? configurationName,
            List<FileAttachment> attachments, bool saveAttachments)
        {
            // No transaction of its own: in the caller's transaction, a rollback here would end the caller's too
            long id;
            using (var command = new SqlCommand(@"
                INSERT INTO EMAIL.MailItems
                    (profileName, configurationName, recipients, copy_recipients, blind_copy_recipients, subject,
                     from_address, reply_to, body, body_format, importance, sensitivity, file_attachments)
                OUTPUT INSERTED.mailitem_id
                VALUES (@profileName, @configurationName, @recipients, @copyRecipients, @bccRecipients, @subject,
                        @fromAddress, @replyTo, @body, @bodyFormat, @importance, @sensitivity, @fileAttachments);", cnn))
            {
                command.Parameters.Add("@profileName", SqlDbType.Char, 20).Value = profileName;
                command.Parameters.Add("@configurationName", SqlDbType.Char, 20).Value = (object?)configurationName ?? DBNull.Value;
                command.Parameters.Add("@recipients", SqlDbType.VarChar, -1).Value = List(message.To);
                command.Parameters.Add("@copyRecipients", SqlDbType.VarChar, -1).Value = Nullable(List(message.CC));
                command.Parameters.Add("@bccRecipients", SqlDbType.VarChar, -1).Value = Nullable(List(message.Bcc));
                command.Parameters.Add("@subject", SqlDbType.NVarChar, 255).Value = message.Subject;
                command.Parameters.Add("@fromAddress", SqlDbType.VarChar, -1).Value = message.From.Address;
                command.Parameters.Add("@replyTo", SqlDbType.VarChar, -1).Value = Nullable(List(message.ReplyToList));
                command.Parameters.Add("@body", SqlDbType.NVarChar, -1).Value = message.Body;
                command.Parameters.Add("@bodyFormat", SqlDbType.VarChar, 20).Value = message.IsBodyHtml ? "HTML" : "TEXT";
                command.Parameters.Add("@importance", SqlDbType.VarChar, 6).Value = message.Priority.ToString();
                command.Parameters.Add("@sensitivity", SqlDbType.VarChar, 50).Value = (object?)message.Headers["Sensitivity"] ?? DBNull.Value;
                command.Parameters.Add("@fileAttachments", SqlDbType.NVarChar, -1).Value = Nullable(Paths(attachments));
                id = (long)command.ExecuteScalar();
            }

            if (saveAttachments)
            {
                foreach (var file in attachments)
                {
                    using (var command = new SqlCommand(@"
                        INSERT INTO EMAIL.MailAttachments (mailitem_id, filename, filesize, attachment)
                        VALUES (@mailItem, @fileName, @fileSize, @attachment);", cnn))
                    {
                        command.Parameters.Add("@mailItem", SqlDbType.BigInt).Value = id;
                        command.Parameters.Add("@fileName", SqlDbType.NVarChar, 512).Value = file.Name;
                        command.Parameters.Add("@fileSize", SqlDbType.BigInt).Value = file.Content.LongLength;
                        command.Parameters.Add("@attachment", SqlDbType.VarBinary, -1).Value = file.Content;
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        private static string List(MailAddressCollection addresses)
        {
            var list = new StringBuilder();
            foreach (var address in addresses)
                list.Append(address.Address).Append(';');
            return list.ToString();
        }

        private static string Paths(List<FileAttachment> attachments)
        {
            var list = new StringBuilder();
            foreach (var file in attachments)
                list.Append(file.Path).Append(';');
            return list.ToString();
        }

        private static object Nullable(string value) => value.Length == 0 ? DBNull.Value : (object)value;

        private static string Cut(string value, int length) => value.Length > length ? value.Substring(0, length) : value;
    }
}
