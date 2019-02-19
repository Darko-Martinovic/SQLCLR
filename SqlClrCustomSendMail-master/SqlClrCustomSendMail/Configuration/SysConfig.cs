// ReSharper disable once CheckNamespace
namespace SqlClrCustomSendMail
{
    public class SysConfig
    {
        // Implemented
        /// <summary>
        /// Maximum file size in bytes. Default 1MB
        /// </summary>
        public int MaxFileSize { get; set; } = 1000000;

// Implemented
        /// <summary>
        /// File extension that are not allowed
        /// </summary>
        public string ProhibitedExtension { get; set; } = "exe,dll,vbs,js";

        public ELoggingLevel LoggingLevel { get; set; } = ELoggingLevel.Minimal;

        /// <summary>
        /// Do we save e-mails?
        /// </summary>
        public bool SaveEmails { get; set; } = true;

        /// <summary>
        /// What is configuration name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// How we send an e-mail?
        /// </summary>
        public bool SendAsync { get; set; } = true;

        /// <summary>
        /// Do we need piping
        /// </summary>
        public bool NoPiping { get; set; } = true;

        /// <summary>
        /// Do we save attachments
        /// </summary>
        public bool SaveAttachments { get; set; } = true;
    }

    public enum ELoggingLevel
    {
        Minimal = 0,
        Usual = 1,
        Maximum = 2
    }
}
