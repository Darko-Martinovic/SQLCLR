using System;
using System.Collections.Generic;
using System.Text;

namespace SqlClrCustomSendMail
{
    public class SysConfig
    {
        //Implemented
        /// <summary>
        /// Maximum file size in bytes. Default 1MB
        /// </summary>
        public Int32 maxFileSize
        {
            get; set;
        } = 1000000;
        //Implemented
        /// <summary>
        /// File extension that are not allowed
        /// </summary>
        public string prohibitedExtension
        {
            get; set;
        } = "exe,dll,vbs,js";
        public eLoggingLevel loggingLevel
        {
            get; set;
        } = eLoggingLevel.Minimal;
        /// <summary>
        /// Do we save e-mails?
        /// </summary>
        public bool saveEmails
        {
            get; set;
        } = true;

        /// <summary>
        /// What is configuration name
        /// </summary>
        public string name
        {
            get; set;
        } = null;
        /// <summary>
        /// How we send an e-mail?
        /// </summary>
        public bool sendAsync
        {
            get; set;
        } = true;
        /// <summary>
        /// Do we need piping
        /// </summary>
        public bool noPiping
        {
            get; set;
        } = true;
        /// <summary>
        /// Do we save attachments
        /// </summary>
        public bool saveAttachments
        {
            get; set;
        } = true;



    }
    public enum eLoggingLevel
    {
        Minimal = 0,
        Usual = 1,
        Maximum = 2
    }
}
