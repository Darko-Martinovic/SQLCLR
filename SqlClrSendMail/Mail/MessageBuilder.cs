using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Text;

namespace SimpleTalk.SqlClr.SendMail
{
    internal static class MessageBuilder
    {
        /// <summary>
        /// Reads the files to attach. A file that can't be attached stops the message: it is
        /// better not to send it than to send it without what it was meant to carry.
        /// </summary>
        public static List<FileAttachment> ReadAttachments(string? fileAttachments, MailConfiguration config)
        {
            var files = new List<FileAttachment>();
            var paths = Addresses.Split(fileAttachments);
            if (paths.Count == 0)
                return files;

            var folders = config.Folders();
            if (folders.Count == 0)
                throw new SendMailException(
                    $"The configuration '{config.Name ?? "built-in"}' allows no attachments: set AttachmentFolders in EMAIL.Configurations to the folders files may be attached from, and pass that configuration as @configName.");

            foreach (var path in paths)
            {
                if (!Path.IsPathRooted(path))
                    throw new SendMailException($"The attachment '{path}' is not a full path.");

                var full = Path.GetFullPath(path);
                if (!InFolder(full, folders))
                    throw new SendMailException($"The attachment '{path}' is not in the configuration's AttachmentFolders.");

                if (config.IsProhibited(Path.GetExtension(full)))
                    throw new SendMailException($"The attachment '{path}' has a prohibited extension.");

                if (!File.Exists(full))
                    throw new SendMailException($"The attachment '{path}' does not exist, or the SQL Server service account can't see it.");

                var size = new FileInfo(full).Length;
                if (size > config.MaxFileSize)
                    throw new SendMailException($"The attachment '{path}' has {size} bytes, more than the configuration's MaxFileSize of {config.MaxFileSize}.");

                files.Add(new FileAttachment(full, File.ReadAllBytes(full)));
            }

            return files;
        }

        public static MailMessage Build(MailRequest request, MailProfile profile, List<FileAttachment> attachments)
        {
            var message = new MailMessage
            {
                From = Address(request.FromAddress ?? profile.DefaultFrom, request.DisplayName ?? profile.DefaultDisplayName),
                Subject = request.Subject,
                Body = request.Body,
                IsBodyHtml = request.BodyHtml,
                SubjectEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8,
                HeadersEncoding = Encoding.UTF8,
                Priority = request.Priority,
                DeliveryNotificationOptions = request.DeliveryNotification
            };

            foreach (var address in Addresses.Split(request.To))
                message.To.Add(Address(address, null));
            foreach (var address in Addresses.Split(request.Cc))
                message.CC.Add(Address(address, null));
            foreach (var address in Addresses.Split(request.Bcc))
                message.Bcc.Add(Address(address, null));
            foreach (var address in Addresses.Split(request.ReplyTo))
                message.ReplyToList.Add(Address(address, null));

            if (message.To.Count == 0)
                throw new SendMailException("@mailTo has no e-mail address.");

            if (request.RequestReadReceipt)
                message.Headers.Add("Disposition-Notification-To",
                    message.ReplyToList.Count > 0 ? message.ReplyToList[0].Address : message.From.Address);

            switch (request.Sensitivity)
            {
                case 0:
                    message.Headers.Add("Sensitivity", "Personal");
                    break;
                case 1:
                    message.Headers.Add("Sensitivity", "Private");
                    break;
                case 2:
                    message.Headers.Add("Sensitivity", "Company-Confidential");
                    break;
            }

            foreach (var file in attachments)
                message.Attachments.Add(new Attachment(new MemoryStream(file.Content), file.Name) { NameEncoding = Encoding.UTF8 });

            return message;
        }

        private static MailAddress Address(string address, string? displayName)
        {
            try
            {
                return displayName == null || displayName.Trim().Length == 0
                    ? new MailAddress(address.Trim())
                    : new MailAddress(address.Trim(), displayName, Encoding.UTF8);
            }
            catch (FormatException)
            {
                throw new SendMailException($"'{address}' is not an e-mail address.");
            }
        }

        private static bool InFolder(string path, List<string> folders)
        {
            foreach (var folder in folders)
                if (path.StartsWith(folder, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }
}
