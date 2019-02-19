using System.Net.Mail;


// ReSharper disable once CheckNamespace
namespace SqlClrCustomSendMail
{
    public class SysProfile
    {
        /// <summary>
        /// An instance of SmptClient
        /// </summary>
        public SmtpClient Client
        {
            get;
            set;
        }
        /// <summary>
        /// Default from address
        /// </summary>
        public string DefaultFromAddress
        {
            get;
            set;
        }
        /// <summary>
        /// Default display name
        /// </summary>
        public string DefaultDisplayName
        {
            get;
            set;
        }
        /// <summary>
        /// Default domain name for Exchange users
        /// </summary>
        public string DefaultDomain
        {
            get;
            set;
        }
        /// <summary>
        /// Default name
        /// </summary>
        public string Name
        {
            get;
            set;
        }
        /// <summary>
        /// Built-in name
        /// </summary>
        public string BuilInName
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Global
            get;
            set;
        }

    }
}
