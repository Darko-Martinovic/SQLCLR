using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net.Mail;
using System.Text;

namespace SqlClrCustomSendMail
{
    public class EmailTracker
    {
        public static bool SaveEmail(MailMessage mm, string profileName, string configurationName,string validAttachments,bool saveAttachments)
        {
            bool retValue = true;
            string cmdString = @"DECLARE @MyTableVar table( mailId bigint)
INSERT INTO [EMAIL].[MailItems]
           ([profileName]
           ,[configurationName]
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
OUTPUT INSERTED.mailitem_id INTO @MyTableVar
     VALUES
           (@profileName
           ,@configurationName
           ,@recipiens
           ,@copyRecipiens
           ,@bccRecipiens
           ,@subject
           ,@fromAddress
           ,@replayToAddresses
           ,@body
           ,@bodyFormat
           ,@importance
           ,@sensitivity
           ,@fileAttachments);SELECT mailId FROM @MyTableVar";

            string connString = "context connection=true";

            using (SqlConnection conn = new SqlConnection(connString))
            {
                using (SqlCommand comm = new SqlCommand())
                {
                    comm.Connection = conn;
                    comm.CommandText = cmdString;
                    //comm.Parameters.AddWithValue("@timeStamp", timeStamp);
                    //comm.Parameters[comm.Parameters.Count - 1].SqlDbType = System.Data.SqlDbType.DateTime;

                    comm.Parameters.AddWithValue("@profileName", profileName);
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = System.Data.SqlDbType.Char;
                    comm.Parameters[comm.Parameters.Count - 1].Size = 20;

                    comm.Parameters.AddWithValue("@configurationName", configurationName);
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = System.Data.SqlDbType.Char;
                    comm.Parameters[comm.Parameters.Count - 1].Size = 20;


                    comm.Parameters.AddWithValue("@recipiens", mm.Recipiens());
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = System.Data.SqlDbType.VarChar;
                    comm.Parameters[comm.Parameters.Count - 1].Size = -1;

                    comm.Parameters.AddWithValue("@copyRecipiens", mm.CopyRecipiens());
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = System.Data.SqlDbType.VarChar;
                    comm.Parameters[comm.Parameters.Count - 1].Size = -1;


                    comm.Parameters.AddWithValue("@bccRecipiens", mm.BccCopyRecipiens());
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = System.Data.SqlDbType.VarChar;
                    comm.Parameters[comm.Parameters.Count - 1].Size = -1;


                    comm.Parameters.AddWithValue("@subject", mm.Subject);
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = System.Data.SqlDbType.NVarChar;
                    comm.Parameters[comm.Parameters.Count - 1].Size = 255;


                    comm.Parameters.AddWithValue("@fromAddress", mm.From.Address);
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = System.Data.SqlDbType.VarChar;
                    comm.Parameters[comm.Parameters.Count - 1].Size = -1;


                    comm.Parameters.AddWithValue("@replayToAddresses", mm.ReplyToAddresses());
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = System.Data.SqlDbType.VarChar;
                    comm.Parameters[comm.Parameters.Count - 1].Size = -1;


                    comm.Parameters.AddWithValue("@body", mm.Body.ToString());
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = System.Data.SqlDbType.NVarChar;
                    comm.Parameters[comm.Parameters.Count - 1].Size = -1;



                    comm.Parameters.AddWithValue("@bodyFormat", mm.IsBodyHtml ? "HTML" : "TEXT");
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = System.Data.SqlDbType.VarChar;
                    comm.Parameters[comm.Parameters.Count - 1].Size = 20;

                    comm.Parameters.AddWithValue("@importance", mm.Priority.ToString());
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = System.Data.SqlDbType.VarChar;
                    comm.Parameters[comm.Parameters.Count - 1].Size = 20;

                    comm.Parameters.AddWithValue("@sensitivity", mm.Headers.Count > 0 && mm.Headers["Sensitivity"] != null ? mm.Headers["Sensitivity"].ToString() : null);
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = System.Data.SqlDbType.VarChar;
                    comm.Parameters[comm.Parameters.Count - 1].Size = 20;

                    comm.Parameters.AddWithValue("@fileAttachments", mm.AttachmentsPath());
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = System.Data.SqlDbType.NVarChar;
                    comm.Parameters[comm.Parameters.Count - 1].Size = -1;


                   try
                    {
                        conn.Open();
                      
                        comm.Transaction = conn.BeginTransaction();
                        Int64 id = (Int64)comm.ExecuteScalar();
                        comm.Parameters.Clear();
                        
                        if (saveAttachments && validAttachments.Trim().Equals(string.Empty) == false)
                        {

                            comm.CommandText = @"INSERT INTO [EMail].[MailAttachments] (mailitem_id,fileName,fileSize,attachment)
                                             SELECT @mailItem, fileName, fileSize, attachment FROM @tvpEmails";

                            comm.Parameters.AddWithValue("@mailItem", id);
                            comm.Parameters[comm.Parameters.Count - 1].SqlDbType = System.Data.SqlDbType.BigInt;

                            DataTable dt = CreateTable();
                            DataRow newRow = null;
                            foreach (Attachment eml in mm.Attachments)
                            {
                                newRow = dt.NewRow();
                                newRow["FileName"] = eml.Name;
                                newRow["FileSize"] = eml.ContentStream.Length;
                                byte[] allBytes = new byte[eml.ContentStream.Length];
                                int bytesRead = eml.ContentStream.Read(allBytes, 0, (int)eml.ContentStream.Length);
                                newRow["Attachment"] = allBytes;
                                eml.ContentStream.Position = 0;
                                dt.Rows.Add(newRow);
                            }
                            comm.Parameters.AddWithValue("@tvpEmails", dt);
                            comm.Parameters[comm.Parameters.Count - 1].TypeName = "EMAIL.TVP_Emails";
                            comm.Parameters[comm.Parameters.Count - 1].SqlDbType = SqlDbType.Structured;
                            comm.ExecuteNonQuery();
                            dt = null;
                            newRow = null;
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                        }

                        comm.Transaction.Commit();
                        if (conn.State == ConnectionState.Open)
                            conn.Close();
                    }
                    catch (SqlException e)
                    {
                        comm.Transaction.Rollback();
                        retValue = false;
                        //TO-DO LOG
                        var error = e.Message;
                        //TO-DO
                    }
                }
            }

            return retValue;
        }

        private static DataTable CreateTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("FileName", typeof(String));
            dt.Columns.Add("FileSize", typeof(Int64));
            dt.Columns.Add("Attachment", typeof(Byte[]));
            return dt;
        }
    }
}
