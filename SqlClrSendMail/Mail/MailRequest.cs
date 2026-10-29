using System.Collections.Generic;
using System.Net.Mail;

namespace SimpleTalk.SqlClr.SendMail
{
    /// <summary>
    /// What CLRSendMail was asked to send, with the defaults applied.
    /// </summary>
    internal sealed class MailRequest
    {
        public string To { get; set; } = "";
        public string Subject { get; set; } = "SQLCLR Server Message";
        public string Body { get; set; } = "";
        public string? FromAddress { get; set; }
        public string? DisplayName { get; set; }
        public string? Cc { get; set; }
        public string? Bcc { get; set; }
        public string? ReplyTo { get; set; }
        public string? FileAttachments { get; set; }
        public bool RequestReadReceipt { get; set; }
        public DeliveryNotificationOptions DeliveryNotification { get; set; } = DeliveryNotificationOptions.None;
        public int Sensitivity { get; set; } = -1;
        public MailPriority Priority { get; set; } = MailPriority.Normal;
        public bool BodyHtml { get; set; } = true;
    }

    /// <summary>A file to attach, read before sending.</summary>
    internal sealed class FileAttachment
    {
        public FileAttachment(string path, byte[] content)
        {
            Path = path;
            Content = content;
        }

        public string Path { get; }
        public string Name => System.IO.Path.GetFileName(Path);
        public byte[] Content { get; }
    }

    internal static class Addresses
    {
        /// <summary>The addresses of a semicolon-delimited list.</summary>
        public static List<string> Split(string? list)
        {
            var addresses = new List<string>();
            if (list == null)
                return addresses;
            foreach (var address in list.Split(';'))
                if (address.Trim().Length > 0)
                    addresses.Add(address.Trim());
            return addresses;
        }
    }
}
