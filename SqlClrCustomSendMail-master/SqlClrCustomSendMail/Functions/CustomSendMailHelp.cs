using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Collections;
using System.Collections.Generic;

public partial class UserDefinedFunctions
{
    [Microsoft.SqlServer.Server.SqlFunction(Name = "CustomSendMailHelp",
    FillRowMethodName = "ConfigTable_FillRow",
    DataAccess = DataAccessKind.None,
    IsDeterministic = true,
    TableDefinition = @"ParametarName nvarchar(100), ParametarDescription_______________________________________________________________________________________________________ nvarchar(max)")]
    public static IEnumerable CustomSendMailHelp(
        SqlString ProcedureName  //
        )
    {
        Dictionary<string, string> list = GetList(ProcedureName.Value);

        return list;

    }
    private static void ConfigTable_FillRow(object obj, out SqlString ParametarName, out SqlString ParametarDescription)
    {
        ParametarName = ((System.Collections.Generic.KeyValuePair<string, string>)(obj)).Key;
        ParametarDescription = ((System.Collections.Generic.KeyValuePair<string, string>)(obj)).Value;
    }

    public static Dictionary<string, string> GetList(string ProcedureName)
    {
        Dictionary<string, string> data = new Dictionary<string, string>();
        if (ProcedureName.ToLower().Equals("email.clrsendmail"))
        {





            //@group[nvarchar](4000) = N'''',

            //@requestReadReceipt[bit] = True,
            //@deliveryNotification[smallint] = 1,
            //@configName[nvarchar](20) = N''''


            data.Add("@profileName", "Is the name of the profile to send the message from. Can be built in or user defined(readable from database)." +
                " @profileName must be specified.");

            data.Add("@mailTo", "Is a semicolon-delimited list of e-mail addresses to send the message to.");

            data.Add("@mailSubject", "Is the subject of the e-mail message. The subject is of type nvarchar(255). If no subject is specified, the default is 'SQLCLR Server Message'");

            data.Add("@mailBody", "Is the body of the e-mail message. The message body is of type nvarchar(max), with a default of NULL");

            data.Add("@fromAddress", "Is the value of the 'from address' of the email message. This is an optional parameter used to override the settings in the mail profile. " +
                " SMTP security settings determine if these overrides are accepted. If no parameter is specified, the default is NULL. ");

            data.Add("@displayName", "Display name. This parameter is connected with parameter @fromAddress");

            data.Add("@mailCC", "Is a semicolon-delimited list of e-mail addresses to carbon copy the message to.");

            data.Add("@blindCopyRec", "Is a semicolon-delimited list of e-mail addresses to blind carbon copy the message to.");

            data.Add("@replyAddress", "Is the value of the 'reply to address' of the email message. It accepts only one email " +
                                      "address as a valid value. This is an optional parameter used to override the settings in the mail profile.");

            data.Add("@fileAttachments", "Is a semicolon-delimited list of file names to attach to the e-mail message. Files in the list must " +
                "be specified as absolute paths. The attachments list is of type nvarchar(max). By default, SQLCLR Mail limits file attachments to 1 MB per file");

            //Not implemented
            data.Add("@requestReadReceipt", "Request read receipt. Not implemented in sp_send_dbmail");

            data.Add("@deliveryNotification", "Delivery notification.Not implemented in sp_send_dbmail.");
            //end

            data.Add("@sensitivity", "Is the sensitivity of the message. " +
                            "The parameter may contain one of the following values: Normal,Personal,Private & Confidential ");

            data.Add("@mailPriorty", "Is the importance of the message." +
                "The parameter may contain one of the following values: Low,Normal & High ");


            data.Add("@bodyHtml", "Is the format of the message body" +
    "Can be HTML or TEXT");

            data.Add("@configName", "Is the name of the configuration. " +
    " Configuration is responsible for protecting system of large file attachements");
        }
        else if (ProcedureName.ToLower().Equals("email.querytohtml"))
        {
            data.Add("@Query", "Query or stored procedure which we will to transform into HTML. Stored procedure always should begin with keyword EXEC.");
            data.Add("@Params", "Query or stored procedure parametars");
            data.Add("@Caption", "Table header");
            data.Add("@Footer", "Table footer. Number of records will be printed when you pass #");
            data.Add("@Rm", "0-means no rotation,1-auto rotation if number of columns is greater then number of rows, 2-means always rotate");
            data.Add("@Rco", "Rotate column ordinal. Valid value is for -1 to max column value. If you specify @Rm parametar = 0 @Rco parametar is ignored.");
            data.Add("@style", "There are 7 predefined styles, namely ST_BLUE, ST_BLACK, ST_BROWN, ST_ROSE,ST_RED,ST_GREEN and ST_SIMPLE. You can pass your custom css stylesheet as well.");
        }
        else if (ProcedureName.ToLower().Equals("email.concathtml")) //ConCatHtml
        {
            data.Add("@mHtml", "Main html string. Should be well formatted. It means should have html, head and body tags.");
            data.Add("@sHtml", "Html string we want to merge with the main html string");
        }
        else
        {
            data.Add("Error", "Procedure or function " + ProcedureName + " does not exists!");
        }

        for (int i = 0; i <= GC.MaxGeneration; i++)
        {
#if NET_4_5
            GC.Collect(i, GCCollectionMode.Forced,true,true);
#elif NET_4_0
            GC.Collect(i, GCCollectionMode.Forced,true);
#elif NET_3_5
            GC.Collect(i, GCCollectionMode.Forced);
#else
            GC.Collect(i, GCCollectionMode.Forced);
#endif
        }

            GC.WaitForPendingFinalizers();

        return data;
    }

}
