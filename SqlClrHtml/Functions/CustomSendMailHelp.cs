using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

namespace SimpleTalk.SqlClr.Html
{
    public partial class UserDefinedFunctions
    {
        /// <summary>
        /// The parameters of EMAIL.CLRSendMail, EMAIL.SaveProfile, EMAIL.QueryToHtml and EMAIL.ConCatHtml.
        /// </summary>
        [SqlFunction(
            Name = "CustomSendMailHelp",
            FillRowMethodName = nameof(FillHelpRow),
            DataAccess = DataAccessKind.None,
            IsDeterministic = true,
            TableDefinition = "ParameterName nvarchar(100), ParameterDescription nvarchar(max)")]
        public static IEnumerable CustomSendMailHelp(SqlString procedureName)
        {
            return GetHelp(procedureName.IsNull ? "" : procedureName.Value);
        }

        private static void FillHelpRow(object obj, out SqlString parameterName, out SqlString parameterDescription)
        {
            var row = (KeyValuePair<string, string>)obj;
            parameterName = row.Key;
            parameterDescription = row.Value;
        }

        public static List<KeyValuePair<string, string>> GetHelp(string procedureName)
        {
            var data = new List<KeyValuePair<string, string>>();
            var name = procedureName.Replace("[", "").Replace("]", "").Trim().ToLowerInvariant();
            if (name.StartsWith("email.", System.StringComparison.Ordinal))
                name = name.Substring("email.".Length);

            switch (name)
            {
                case "clrsendmail":
                    Add(data, "@profileName", "The profile in EMAIL.Profiles to send the message with: its SMTP server and account. Required.");
                    Add(data, "@mailTo", "A semicolon-delimited list of e-mail addresses to send the message to. Required.");
                    Add(data, "@mailSubject", "The subject, nvarchar(255). Default is 'SQLCLR Server Message'.");
                    Add(data, "@mailBody", "The body, nvarchar(max), such as the HTML of EMAIL.QueryToHtml. Required.");
                    Add(data, "@fromAddress", "The from address, instead of the profile's. Default is null.");
                    Add(data, "@displayName", "The from address's display name, instead of the profile's. Default is null.");
                    Add(data, "@mailCc", "A semicolon-delimited list of e-mail addresses to carbon copy the message to. Default is null.");
                    Add(data, "@blindCopyRec", "A semicolon-delimited list of e-mail addresses to blind carbon copy the message to. Default is null.");
                    Add(data, "@replyAddress", "A semicolon-delimited list of reply-to addresses. Default is null.");
                    Add(data, "@fileAttachments", "A semicolon-delimited list of files to attach, as full paths the SQL Server service account can read, in a folder of the configuration's AttachmentFolders. Default is null.");
                    Add(data, "@requestReadReceipt", "1 to ask for a read receipt, sent to the first reply-to address or the from address. Default is 0.");
                    Add(data, "@deliveryNotification", "Delivery status notifications: 0 none, 1 on success, 2 on failure, 4 on delay, or their sum. Default is 0.");
                    Add(data, "@sensitivity", "-1 none, 0 Personal, 1 Private, 2 Company-Confidential. Default is -1.");
                    Add(data, "@mailPriorty", "The importance: 0 normal, 1 low, 2 high. Default is 0.");
                    Add(data, "@bodyHtml", "1 when the body is HTML, 0 for text. Default is 1.");
                    Add(data, "@configName", "The configuration in EMAIL.Configurations: attachment limits, logging and what is saved. Default is the built-in one.");
                    break;
                case "saveprofile":
                    Add(data, "@profileName", "The profile's name, up to 20 characters. A profile of that name is replaced.");
                    Add(data, "@hostName", "The SMTP server.");
                    Add(data, "@port", "The SMTP port: 587 or 25. Port 465 (TLS from the start) is not supported by .NET Framework. Default is 587.");
                    Add(data, "@enableSsl", "1 to switch to TLS with STARTTLS; the server's certificate must be valid. Default is 1.");
                    Add(data, "@userName", "The account to sign in with. Default is null, for none.");
                    Add(data, "@password", "The account's password, up to 120 characters, kept encrypted with the database's certificate EmailProfiles. Null keeps the profile's password; an empty string removes it. Default is null.");
                    Add(data, "@defaultFrom", "The from address of the profile's messages.");
                    Add(data, "@defaultDisplayName", "The from address's display name. Default is null.");
                    Add(data, "@useDefaultCredentials", "1 to sign in as the SQL Server service account instead (Windows authentication). Default is 0.");
                    Add(data, "@domain", "The account's Windows domain, for Exchange. Default is null.");
                    Add(data, "@timeout", "How long to wait for the server, in milliseconds. Default is 100000.");
                    break;
                case "querytohtml":
                    Add(data, "@query", "The query whose results become HTML tables. A function can run no stored procedure but a few system ones: put a procedure's result in a temporary table with INSERT ... EXEC, and query that.");
                    Add(data, "@Params", "The query's parameters, declared as in DECLARE: @id int = 1, @name nvarchar(50) = N'Bikes'.");
                    Add(data, "@caption", "Each table's caption.");
                    Add(data, "@footer", "Each table's footer; # gives the number of rows.");
                    Add(data, "@rm", "Rotate mode: 0 no rotation, 1 rotate when there are fewer rows than columns, 2 always rotate.");
                    Add(data, "@rco", "Rotate column ordinal: the column, counted from 0, whose values become the header, or -1 for a Key, Value header. Used only when a table is rotated.");
                    Add(data, "@style", "ST_BLUE, ST_RED, ST_GREEN, ST_ROSE, ST_BROWN, ST_BLACK, ST_SIMPLE or ST_NOSTYLE, or a style element of your own. Default is ST_BLUE.");
                    break;
                case "concathtml":
                    Add(data, "@mHtml", "The main HTML, from QueryToHtml or ConCatHtml.");
                    Add(data, "@sHtml", "The HTML to add after it, from QueryToHtml or ConCatHtml.");
                    break;
                default:
                    Add(data, "Error", $"The procedure or the function {procedureName} does not exist!");
                    break;
            }

            return data;
        }

        private static void Add(List<KeyValuePair<string, string>> data, string key, string value) =>
            data.Add(new KeyValuePair<string, string>(key, value));
    }
}
