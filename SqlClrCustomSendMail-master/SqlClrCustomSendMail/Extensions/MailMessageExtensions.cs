using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;

namespace SqlClrCustomSendMail
{
    public static class MailMessageExtension
    {
        public static string HeaderInformation(this MailMessage mm)
        {
            return @"From     : " + "'" + mm.From.Address.Trim() + "'" + "\r\n" +
                    "To       : " + "'" + mm.To[0].Address.ToString().Trim() + "'" + "\r\n" +
                    "Subject  : " + "'" + mm.Subject.ToString().Trim() + "'";
        }
        public static string Recipiens(this MailMessage mm)
        {
            string retValue = string.Empty;
            foreach (MailAddress eml in mm.To)
            {
                retValue += eml.Address.ToString() + ";";
            }
            return retValue;
        }

        public static string CopyRecipiens(this MailMessage mm)
        {
            string retValue = null;
            foreach (MailAddress eml in mm.CC)
            {
                if (retValue == null)
                    retValue = string.Empty;
                retValue += eml.Address.ToString() + ";";
            }
            return retValue;
        }
        public static string BccCopyRecipiens(this MailMessage mm)
        {
            string retValue = null;
            foreach (MailAddress eml in mm.Bcc)
            {
                if (retValue == null)
                    retValue = string.Empty;
                retValue += eml.Address.ToString() + ";";
            }
            return retValue;
        }


        public static string ReplyToAddresses(this MailMessage mm)
        {
            string retValue = null;
            foreach (MailAddress eml in mm.ReplyToList)
            {
                if (retValue == null)
                    retValue = string.Empty;
                retValue += eml.Address.ToString() + ";";
            }
            return retValue;
        }


        public static string AttachmentsPath(this MailMessage mm)
        {
            string retValue = null;
            foreach (Attachment eml in mm.Attachments)
            {
                if (retValue == null)
                    retValue = string.Empty;
                retValue += eml.Name + ";";
            }
            return retValue;
        }







    }
}




