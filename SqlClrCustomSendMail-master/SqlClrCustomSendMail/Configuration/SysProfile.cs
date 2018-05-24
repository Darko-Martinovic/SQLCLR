using System.Net.Mail;


namespace SqlClrCustomSendMail
{
    public class SysProfile
    {
        /// <summary>
        /// An instance of SmptClient
        /// </summary>
        public SmtpClient Client
        {
            get; set;
        } = null;
        /// <summary>
        /// Default from address
        /// </summary>
        public string DefaultFromAddress
        {
            get; set;
        } = null;
        /// <summary>
        /// Default display name
        /// </summary>
        public string DefaultDisplayName
        {
            get; set;
        } = null;
        /// <summary>
        /// Default domain name for Exchange users
        /// </summary>
        public string DefaultDomain
        {
            get; set;
        } = null;
        /// <summary>
        /// Default name
        /// </summary>
        public string Name
        {
            get; set;
        } = null;
        /// <summary>
        /// Built-in name
        /// </summary>
        public string BuilInName
        {
            get; set;
        } = null;

    }
}
