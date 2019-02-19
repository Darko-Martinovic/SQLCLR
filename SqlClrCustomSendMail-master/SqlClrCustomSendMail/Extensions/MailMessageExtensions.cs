using System.Net.Mail;

// ReSharper disable once CheckNamespace
namespace SqlClrCustomSendMail
{
    public static class MailMessageExtension
    {
        public static string HeaderInformation(this MailMessage mm)
        {
            return $@"From     : '{mm.From.Address.Trim()}'
To       : '{mm.To[0].Address.Trim()}'
Subject  : '{mm.Subject.Trim()}'";
        }
        public static string Recipiens(this MailMessage mm)
        {
            var retValue = string.Empty;
            foreach (var eml in mm.To)
            {
                retValue += eml.Address + ";";
            }
            return retValue;
        }

        public static string CopyRecipiens(this MailMessage mm)
        {
            string retValue = null;
            foreach (var eml in mm.CC)
            {
                if (retValue == null)
                    retValue = string.Empty;
                retValue += eml.Address + ";";
            }
            return retValue;
        }
        public static string BccCopyRecipiens(this MailMessage mm)
        {
            string retValue = null;
            foreach (var eml in mm.Bcc)
            {
                if (retValue == null)
                    retValue = string.Empty;
                retValue += eml.Address + ";";
            }
            return retValue;
        }


        public static string ReplyToAddresses(this MailMessage mm)
        {
            string retValue = null;
            foreach (var eml in mm.ReplyToList)
            {
                if (retValue == null)
                    retValue = string.Empty;
                retValue += eml.Address + ";";
            }
            return retValue;
        }


        public static string AttachmentsPath(this MailMessage mm)
        {
            string retValue = null;
            foreach (var eml in mm.Attachments)
            {
                if (retValue == null)
                    retValue = string.Empty;
                retValue += eml.Name + ";";
            }
            return retValue;
        }







    }
}




