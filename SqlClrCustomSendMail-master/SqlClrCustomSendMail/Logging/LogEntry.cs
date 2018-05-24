using System.Data.SqlClient;

namespace SqlClrCustomSendMail
{
    public static class LogEntry
    {

        public static bool LogItem(string source, string type, string message, string data)
        {
            var retvalue = true;


            using (var conn = new SqlConnection("context connection=true"))
            {
                using (var comm = new SqlCommand())
                {
                    comm.Connection = conn;
                    comm.CommandText = @"INSERT INTO [EMAIL].[MONITORLOG](SOURCE, TYPE,  MESSAGE, DATA)
                                VALUES (@source, @type, @message, @data)";
                    //comm.Parameters.AddWithValue("@timeStamp", timeStamp);
                    //comm.Parameters[comm.Parameters.Count - 1].SqlDbType = System.Data.SqlDbType.DateTime;

                    comm.Parameters.AddWithValue("@source",source);
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = System.Data.SqlDbType.NVarChar;
                    comm.Parameters[comm.Parameters.Count - 1].Size = 50;

                    comm.Parameters.AddWithValue("@type", type);
                    comm.Parameters[comm.Parameters.Count - 1].SqlDbType = System.Data.SqlDbType.NVarChar;
                    comm.Parameters[comm.Parameters.Count - 1].Size = 20;

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
                    catch  (SqlException e)
                    {
                        retvalue = false;
                        //TO-DO LOG ERROR
                        var error = e.Message;
                        //TO-DO
                    }
                }
            }


            return retvalue;
        }
    }
}
