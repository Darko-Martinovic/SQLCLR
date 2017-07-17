using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;

namespace SqlClrCustomSendMail
{
    public class SysProfile
    {
        /// <summary>
        /// An instance of SmptClient
        /// </summary>
        public SmtpClient client
        {
            get; set;
        } = null;
        /// <summary>
        /// Default from address
        /// </summary>
        public string defaultFromAddress
        {
            get; set;
        } = null;
        /// <summary>
        /// Default display name
        /// </summary>
        public string defaultDisplayName
        {
            get; set;
        } = null;
        /// <summary>
        /// Default domain name for Exchange users
        /// </summary>
        public string defaultDomain
        {
            get; set;
        } = null;
        /// <summary>
        /// Default name
        /// </summary>
        public string name
        {
            get; set;
        } = null;
        /// <summary>
        /// Built-in name
        /// </summary>
        public string builInName
        {
            get; set;
        } = null;

    }
}
