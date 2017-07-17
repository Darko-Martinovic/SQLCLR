using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net.Mail;
using System.Text;

namespace SqlClrCustomSendMail
{
    public class EmailTracker
    {
        public static bool SaveEmail(MailMessage mm, int profileId, int configurationId)
        {
            bool retValue = true;
            string cmdString = @"INSERT INTO [EMAIL].[MailItems]
           ([profile_id]
           ,[configuration_id]
           ,[recipients]
           ,[copy_recipients]
           ,[blind_copy_recipients]
           ,[subject]
           ,[from_address]
           ,[reply_to]
           ,[body]
           ,[body_format]
           ,[importance]
           ,[sensitivity]
           ,[file_attachments])
     VALUES
           (<profile_id, int,>
           ,<configuration_id, int,>
           ,<recipients, varchar(max),>
           ,<copy_recipients, varchar(max),>
           ,<blind_copy_recipients, varchar(max),>
           ,<subject, nvarchar(255),>
           ,<from_address, varchar(max),>
           ,<reply_to, varchar(max),>
           ,<body, nvarchar(max),>
           ,<body_format, varchar(20),>
           ,<importance, varchar(6),>
           ,<sensitivity, varchar(12),>
           ,<file_attachments, nvarchar(max),>)
";

            string connString = "context connection=true";

            using (SqlConnection conn = new SqlConnection(connString))
            {
                using (SqlCommand comm = new SqlCommand())
                {
                    comm.Connection = conn;
                    comm.CommandText = cmdString;
                    //comm.Parameters.AddWithValue("@timeStamp", timeStamp);
                    //comm.Parameters[comm.Parameters.Count - 1].SqlDbType = System.Data.SqlDbType.DateTime;

                    comm.Parameters.AddWithValue("@profileId", profileId);
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = System.Data.SqlDbType.BigInt;

                    comm.Parameters.AddWithValue("@category", category);
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = System.Data.SqlDbType.SmallInt;

                    comm.Parameters.AddWithValue("@type", type);
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = System.Data.SqlDbType.NVarChar;
                    comm.Parameters[comm.Parameters.Count - 1].Size = 20;

                    comm.Parameters.AddWithValue("@eventId", eventid);
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = System.Data.SqlDbType.Int;

                    comm.Parameters.AddWithValue("@message", message);
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = System.Data.SqlDbType.NVarChar;
                    comm.Parameters[comm.Parameters.Count - 1].Size = 100;

                    comm.Parameters.AddWithValue("@data", data);
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = System.Data.SqlDbType.NVarChar;
                    comm.Parameters[comm.Parameters.Count - 1].Size = -1;

                    try
                    {
                        conn.Open();
                        comm.ExecuteNonQuery();
                    }
                    catch (SqlException e)
                    {
                        retValue = false;
                    }
                }
            }

            return retValue;
        }
    }
}
