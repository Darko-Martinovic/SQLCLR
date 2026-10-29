using System.Collections.Generic;

namespace SimpleTalk.SqlClr.Html
{
    /// <summary>
    /// A result as text: its column names and its rows, with null for NULL.
    /// </summary>
    public sealed class ResultTable
    {
        public ResultTable(IList<string> columns)
        {
            Columns = columns;
        }

        public IList<string> Columns { get; }

        public List<string?[]> Rows { get; } = new List<string?[]>();
    }
}
