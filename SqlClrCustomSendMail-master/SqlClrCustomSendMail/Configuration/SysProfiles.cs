using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;

namespace SqlClrCustomSendMail
{
    public class SysProfiles
    {
        public SmtpClient client
        {
            get; set;
        } = null;
        public string defaultFromAddress
        {
            get; set;
        } = null;
        public string defaultGroupName
        {
            get; set;
        } = null;
        public string defaultDomain
        {
            get; set;
        } = null;
        public string name
        {
            get; set;
        } = null;
        public string builInName
        {
            get; set;
        } = null;

    }
}
