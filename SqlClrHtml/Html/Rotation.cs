namespace SimpleTalk.SqlClr.Html
{
    /// <summary>
    /// Transposing a result, as MATRIX.Transposing does: its columns become rows.
    /// </summary>
    internal static class Rotation
    {
        /// <summary>
        /// The values of column rco become the header; every other column becomes a row.
        /// </summary>
        public static ResultTable OnColumn(ResultTable table, int rco)
        {
            if (rco < 0 || rco >= table.Columns.Count)
                throw new HtmlException(
                    $"@rco is {rco}, but the result's columns are numbered 0 to {table.Columns.Count - 1}, or -1 for a Key, Value header.");

            var header = new string[table.Rows.Count + 1];
            header[0] = table.Columns[rco];
            for (var r = 0; r < table.Rows.Count; r++)
                header[r + 1] = table.Rows[r][rco] ?? "";

            var rotated = new ResultTable(header);
            for (var c = 0; c < table.Columns.Count; c++)
                if (c != rco)
                    rotated.Rows.Add(Line(table, c));
            return rotated;
        }

        /// <summary>
        /// A generic header, Key, Value, Value1..., and every column as a row.
        /// </summary>
        public static ResultTable KeyValue(ResultTable table)
        {
            var header = new string[table.Rows.Count + 1];
            header[0] = "Key";
            for (var r = 0; r < table.Rows.Count; r++)
                header[r + 1] = r == 0 ? "Value" : "Value" + r;

            var rotated = new ResultTable(header);
            for (var c = 0; c < table.Columns.Count; c++)
                rotated.Rows.Add(Line(table, c));
            return rotated;
        }

        private static string?[] Line(ResultTable table, int column)
        {
            var line = new string?[table.Rows.Count + 1];
            line[0] = table.Columns[column];
            for (var r = 0; r < table.Rows.Count; r++)
                line[r + 1] = table.Rows[r][column];
            return line;
        }
    }
}
