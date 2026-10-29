using System.Collections.Generic;
using System.Data.SqlClient;

namespace SimpleTalk.SqlClr.Html
{
    internal static class DataAccess
    {
        // We must use "context connection=true": the caller's connection, permissions and transaction
        private const string ContextConnection = "context connection=true";

        /// <summary>
        /// Runs the batch and reads every result it returns as text.
        /// </summary>
        public static List<ResultTable> GetResults(string batch)
        {
            var results = new List<ResultTable>();
            using (var cnn = new SqlConnection(ContextConnection))
            using (var command = new SqlCommand(batch, cnn))
            {
                cnn.Open();
                using (var reader = command.ExecuteReader())
                {
                    do
                    {
                        //
                        // Statements without a result, such as SET, have no columns
                        //
                        if (reader.FieldCount == 0)
                            continue;

                        var columns = new string[reader.FieldCount];
                        for (var i = 0; i < columns.Length; i++)
                            columns[i] = reader.GetName(i);

                        var formatter = new ValueFormatter(reader);
                        var table = new ResultTable(columns);
                        while (reader.Read())
                        {
                            var row = new string?[columns.Length];
                            for (var i = 0; i < row.Length; i++)
                                row[i] = formatter.Format(reader, i);
                            table.Rows.Add(row);
                        }

                        results.Add(table);
                    } while (reader.NextResult());
                }
            }

            return results;
        }
    }
}
