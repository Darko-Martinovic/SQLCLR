using System.Data;
using System.Data.SqlClient;
using Microsoft.SqlServer.Server;

namespace SimpleTalk.SqlClr.SendMail
{
    internal static class PipeUtilities
    {
        // RAISERROR takes a message of up to 2047 characters
        private const int MaxMessageLength = 2047;

        /// <summary>
        /// A message, as PRINT sends it.
        /// </summary>
        public static void Print(string message) => SqlContext.Pipe.Send(message);

        /// <summary>
        /// Raises error 50000, where a message from Pipe.Send would only be printed: the client
        /// gets an error and @@ERROR is set, but T-SQL's TRY...CATCH doesn't see an error sent
        /// this way. RAISERROR, unlike THROW, lets the rest of the caller's batch run, as a failed
        /// statement does.
        /// </summary>
        public static void RaiseError(SqlConnection cnn, string message)
        {
            if (message.Length > MaxMessageLength)
                message = message.Substring(0, MaxMessageLength);

            using (var command = new SqlCommand("RAISERROR(N'%s', 16, 1, @message);", cnn))
            {
                command.Parameters.Add("@message", SqlDbType.NVarChar, MaxMessageLength).Value = message;
                try
                {
                    SqlContext.Pipe.ExecuteAndSend(command);
                }
                catch (SqlException)
                {
                    // The error has reached the caller; letting it out of .NET would add error 6522 to it
                }
            }
        }
    }
}
