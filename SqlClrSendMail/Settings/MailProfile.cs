using System;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Net.Mail;

namespace SimpleTalk.SqlClr.SendMail
{
    /// <summary>
    /// A row of EMAIL.Profiles: the SMTP server, the account and the default from address. The
    /// password is kept encrypted with the database's certificate EmailProfiles.
    /// </summary>
    internal sealed class MailProfile
    {
        private MailProfile(string name, string host, int port, bool enableSsl, bool useDefaultCredentials,
            string? userName, string? password, string? domain, string defaultFrom, string? defaultDisplayName, int timeout)
        {
            Name = name;
            Host = host;
            Port = port;
            EnableSsl = enableSsl;
            UseDefaultCredentials = useDefaultCredentials;
            UserName = userName;
            Password = password;
            Domain = domain;
            DefaultFrom = defaultFrom;
            DefaultDisplayName = defaultDisplayName;
            Timeout = timeout;
        }

        public string Name { get; }
        public string Host { get; }
        public int Port { get; }
        public bool EnableSsl { get; }
        public bool UseDefaultCredentials { get; }
        public string? UserName { get; }
        public string? Password { get; }
        public string? Domain { get; }
        public string DefaultFrom { get; }
        public string? DefaultDisplayName { get; }
        public int Timeout { get; }

        public SmtpClient CreateClient()
        {
            var client = new SmtpClient(Host, Port)
            {
                EnableSsl = EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = Timeout,
                UseDefaultCredentials = UseDefaultCredentials
            };
            if (!UseDefaultCredentials && UserName != null)
                client.Credentials = Domain == null
                    ? new NetworkCredential(UserName, Password)
                    : new NetworkCredential(UserName, Password, Domain);
            return client;
        }

        public static MailProfile Load(SqlConnection cnn, string name)
        {
            // CLRSendMail runs as the schema's owner, who may use the certificate; its callers needn't
            using (var command = new SqlCommand(@"
                SELECT TOP (1) ProfileName, EnableSsl, DefaultCred, HostName, Port, UserName,
                       CONVERT(nvarchar(256), DECRYPTBYCERT(CERT_ID(N'EmailProfiles'), Password)) AS Password,
                       CAST(CASE WHEN Password IS NULL THEN 0 ELSE 1 END AS bit) AS HasPassword,
                       Domain, DefaultFrom, DefaultGroup, DeliveryMethod, TimeOut
                FROM EMAIL.Profiles
                WHERE ProfileName = @name;", cnn))
            {
                command.Parameters.Add("@name", SqlDbType.Char, 20).Value = name;
                using (var dr = command.ExecuteReader())
                {
                    if (!dr.Read())
                        throw new SendMailException($"The profile '{name}' is not in EMAIL.Profiles. Add it with EMAIL.SaveProfile.");

                    var password = dr["Password"] as string;
                    if (password == null && (bool)dr["HasPassword"])
                        throw new SendMailException(
                            $"The password of the profile '{name}' can't be decrypted: the certificate EmailProfiles is missing or isn't the one it was saved with. Save the profile again with EMAIL.SaveProfile.");

                    var deliveryMethod = Convert.ToInt32(dr["DeliveryMethod"]);
                    if (deliveryMethod != (int)SmtpDeliveryMethod.Network)
                        throw new SendMailException($"The profile '{name}' has DeliveryMethod {deliveryMethod}: only 0, an SMTP server, is supported.");

                    return new MailProfile(
                        ((string)dr["ProfileName"]).Trim(),
                        (string)dr["HostName"],
                        (int)dr["Port"],
                        (bool)dr["EnableSsl"],
                        (bool)dr["DefaultCred"],
                        dr["UserName"] as string,
                        password,
                        dr["Domain"] as string,
                        (string)dr["DefaultFrom"],
                        dr["DefaultGroup"] as string,
                        (int)dr["TimeOut"]);
                }
            }
        }

        /// <summary>
        /// TLS 1.2 at least. A server whose .NET Framework defaults to the system's choice keeps
        /// it; one pinned to older versions gets TLS 1.2 added.
        /// </summary>
        public static void RequireModernTls()
        {
            var protocols = ServicePointManager.SecurityProtocol;
            if (protocols != SecurityProtocolType.SystemDefault && (protocols & SecurityProtocolType.Tls12) == 0)
                ServicePointManager.SecurityProtocol = protocols | SecurityProtocolType.Tls12;
        }

        public static string Describe(Exception ex)
        {
            var message = ex.Message;
            for (var inner = ex.InnerException; inner != null; inner = inner.InnerException)
                message += " " + inner.Message;
            return message;
        }
    }
}
