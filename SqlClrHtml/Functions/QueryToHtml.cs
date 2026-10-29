using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

namespace SimpleTalk.SqlClr.Html
{
    public partial class UserDefinedFunctions
    {
        /// <summary>
        /// Every result of a query as an HTML table, in one HTML document for an e-mail's body.
        /// </summary>
        [SqlFunction(
            Name = "QueryToHtml",
            DataAccess = DataAccessKind.Read,
            SystemDataAccess = SystemDataAccessKind.Read,
            IsDeterministic = false)]
        public static SqlString QueryToHtml
            (
            SqlString query,       // the query; a function can run no stored procedure but a few system ones
            SqlString parameters,  // the query's parameters, declared as in DECLARE: @id int = 1
            SqlString caption,     // each table's caption
            SqlString footer,      // each table's footer; # for the number of rows
            SqlInt16 rm,           // rotate mode: 0 none, 1 when there are fewer rows than columns, 2 always
            SqlInt16 rco,          // rotate column ordinal: the column whose values become the header, or -1 for Key, Value
            SqlString style        // a built-in style, ST_BLUE by default, or a style element of your own
            )
        {
            if (query.IsNull || query.Value.Trim().Length == 0)
                throw new HtmlException("@query is required.");

            var mode = rm.IsNull ? 0 : rm.Value;
            if (mode < 0 || mode > 2)
                throw new HtmlException($"@rm is {mode}: use 0 for no rotation, 1 to rotate when there are fewer rows than columns, or 2 to always rotate.");

            var tableStyle = TableStyle.Parse(style.IsNull ? null : style.Value);

            //
            // The parameters are a DECLARE list, so SQL Server itself parses their types and values
            //
            var batch = parameters.IsNull || parameters.Value.Trim().Length == 0
                ? query.Value
                : $"DECLARE {parameters.Value};\n{query.Value}";

            var results = Run(batch);
            if (results.Count == 0)
                throw new HtmlException("The query returned no result to show.");

            var html = HtmlDocument.Create(tableStyle);
            foreach (var result in results)
            {
                var rotate = mode == 2 || (mode == 1 && result.Rows.Count < result.Columns.Count);
                var shown = !rotate
                    ? result
                    : !rco.IsNull && rco.Value == -1
                        ? Rotation.KeyValue(result)
                        : Rotation.OnColumn(result, rco.IsNull ? 0 : rco.Value);

                var footerText = footer.IsNull ? null
                    : footer.Value == "#" ? $"Total records : {result.Rows.Count}"
                    : footer.Value;
                HtmlDocument.AddTable(html, shown, caption.IsNull ? null : caption.Value, footerText, tableStyle);
            }

            return HtmlDocument.Write(html);
        }

        private static List<ResultTable> Run(string batch)
        {
            try
            {
                return DataAccess.GetResults(batch);
            }
            catch (SqlException ex)
            {
                throw new HtmlException($"The query failed: {ex.Message}");
            }
        }
    }

    /// <summary>A mistake in a function's arguments, or a failed query, reported as it is.</summary>
    [Serializable]
    public sealed class HtmlException : Exception
    {
        public HtmlException(string message) : base(message)
        {
        }
    }
}
