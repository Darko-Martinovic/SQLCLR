using System;
using System.Data;
using System.Data.SqlClient;
using System.Net.Mail;
// ReSharper disable RedundantAssignment

// ReSharper disable once CheckNamespace
namespace SqlClrCustomSendMail
{
    public static class EmailTracker
    {
        public static bool SaveEmail(
                                MailMessage mm, 
                                string profileName, 
                                string configurationName,
                                string validAttachments,
                                bool saveAttachments
            )

        {
            var retValue = true;


            using (var conn = new SqlConnection("context connection=true"))
            {
                using (var comm = new SqlCommand())
                {
                    comm.Connection = conn;
                    comm.CommandText = @"DECLARE @MyTableVar table( mailId bigint)
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
                    //comm.Parameters.AddWithValue("@timeStamp", timeStamp);
                    //comm.Parameters[comm.Parameters.Count - 1].SqlDbType = System.Data.SqlDbType.DateTime;

                    comm.Parameters.AddWithValue("@profileName", profileName);
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = SqlDbType.Char;
                    comm.Parameters[comm.Parameters.Count - 1].Size = 20;

                    comm.Parameters.AddWithValue("@configurationName", configurationName);
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = SqlDbType.Char;
                    comm.Parameters[comm.Parameters.Count - 1].Size = 20;


                    comm.Parameters.AddWithValue("@recipiens", mm.Recipiens());
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = SqlDbType.VarChar;
                    comm.Parameters[comm.Parameters.Count - 1].Size = -1;

                    comm.Parameters.AddWithValue("@copyRecipiens", mm.CopyRecipiens());
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = SqlDbType.VarChar;
                    comm.Parameters[comm.Parameters.Count - 1].Size = -1;


                    comm.Parameters.AddWithValue("@bccRecipiens", mm.BccCopyRecipiens());
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = SqlDbType.VarChar;
                    comm.Parameters[comm.Parameters.Count - 1].Size = -1;


                    comm.Parameters.AddWithValue("@subject", mm.Subject);
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = SqlDbType.NVarChar;
                    comm.Parameters[comm.Parameters.Count - 1].Size = 255;


                    comm.Parameters.AddWithValue("@fromAddress", mm.From.Address);
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = SqlDbType.VarChar;
                    comm.Parameters[comm.Parameters.Count - 1].Size = -1;


                    comm.Parameters.AddWithValue("@replayToAddresses", mm.ReplyToAddresses());
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = SqlDbType.VarChar;
                    comm.Parameters[comm.Parameters.Count - 1].Size = -1;


                    comm.Parameters.AddWithValue("@body", mm.Body);
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = SqlDbType.NVarChar;
                    comm.Parameters[comm.Parameters.Count - 1].Size = -1;



                    comm.Parameters.AddWithValue("@bodyFormat", mm.IsBodyHtml ? "HTML" : "TEXT");
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = SqlDbType.VarChar;
                    comm.Parameters[comm.Parameters.Count - 1].Size = 20;

                    comm.Parameters.AddWithValue("@importance", mm.Priority.ToString());
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = SqlDbType.VarChar;
                    comm.Parameters[comm.Parameters.Count - 1].Size = 20;

                    comm.Parameters.AddWithValue("@sensitivity", mm.Headers.Count > 0 && mm.Headers["Sensitivity"] != null ? mm.Headers["Sensitivity"] : null);
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = SqlDbType.VarChar;
                    comm.Parameters[comm.Parameters.Count - 1].Size = 20;

                    comm.Parameters.AddWithValue("@fileAttachments", mm.AttachmentsPath());
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = SqlDbType.NVarChar;
                    comm.Parameters[comm.Parameters.Count - 1].Size = -1;


                   try
                    {
                        conn.Open();
                      
                        comm.Transaction = conn.BeginTransaction();
                        var id = (long)comm.ExecuteScalar();
                        comm.Parameters.Clear();
                        
                        if (saveAttachments && validAttachments.Trim().Equals(string.Empty) == false)
                        {

                            comm.CommandText =
                                @"INSERT INTO [EMail].[MailAttachments] (mailitem_id,fileName,fileSize,attachment)
                                             SELECT @mailItem, fileName, fileSize, attachment FROM @tvpEmails";

                            comm.Parameters.AddWithValue("@mailItem", id);
                            comm.Parameters[comm.Parameters.Count - 1].SqlDbType = SqlDbType.BigInt;

                            var dt = CreateTable();
                            DataRow newRow ;
                            foreach (var eml in mm.Attachments)
                            {
                                newRow = dt.NewRow();
                                newRow["FileName"] = eml.Name;
                                newRow["FileSize"] = eml.ContentStream.Length;
                                var allBytes = new byte[eml.ContentStream.Length];
                                //var bytesRead = eml.ContentStream.Read(allBytes, 0, (int)eml.ContentStream.Length);
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
                    catch (SqlException)
                    {
                        comm.Transaction.Rollback();
                        retValue = false;
                    }
                }
            }

            return retValue;
        }

        private static DataTable CreateTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("FileName", typeof(string));
            dt.Columns.Add("FileSize", typeof(long));
            dt.Columns.Add("Attachment", typeof(byte[]));
            return dt;
        }
    }
}
