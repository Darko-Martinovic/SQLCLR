using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace SimpleTalk.SqlClr.Html
{
    /// <summary>
    /// Builds the HTML as XML elements, so every value is escaped and the result is well formed:
    /// ConCatHtml parses it again.
    /// </summary>
    internal static class HtmlDocument
    {
        /// <summary>An empty document: html, a head with the character set and a custom style sheet, and a body.</summary>
        public static XElement Create(TableStyle style)
        {
            var head = new XElement("head", new XElement("meta", new XAttribute("charset", "utf-8")));
            if (style.StyleSheet != null)
                head.Add(ParseStyleSheet(style.StyleSheet));
            return new XElement("html", head, new XElement("body"));
        }

        public static void AddTable(XElement html, ResultTable table, string? caption, string? footer, TableStyle style)
        {
            var element = new XElement("table", Style(style.Table));
            if (style.ClassName != null)
                element.Add(new XAttribute("class", style.ClassName));

            if (!string.IsNullOrEmpty(caption))
                element.Add(new XElement("caption", Style(style.Caption), Text(caption)));

            var header = new XElement("tr");
            foreach (var column in table.Columns)
                header.Add(new XElement("th", Style(style.Header), Text(column)));
            element.Add(new XElement("thead", header));

            var body = new XElement("tbody");
            for (var r = 0; r < table.Rows.Count; r++)
            {
                var row = new XElement("tr");
                foreach (var value in table.Rows[r])
                    row.Add(new XElement("td", Style(style.Data(r)), Text(value)));
                body.Add(row);
            }
            element.Add(body);

            if (!string.IsNullOrEmpty(footer))
                element.Add(new XElement("tfoot",
                    new XElement("tr", new XElement("td", Style(style.Footer), new XAttribute("colspan", table.Columns.Count), Text(footer)))));

            // Version 1 put each built-in style's table in a div of class datagridST_...
            html.Element("body")!.Add(style.StyleSheet == null
                ? new XElement("div", new XAttribute("class", "datagrid" + style.Name), element)
                : element);
        }

        public static string Write(XElement html) => html.ToString(SaveOptions.DisableFormatting);

        private static XAttribute? Style(string? css) => css == null ? null : new XAttribute("style", css);

        private static XElement ParseStyleSheet(string styleSheet)
        {
            try
            {
                var element = XElement.Parse(styleSheet.Trim());
                if (element.Name.LocalName != "style")
                    throw new HtmlException("@style must be a style element: <style type=\"text/css\">...</style>.");
                return element;
            }
            catch (XmlException ex)
            {
                throw new HtmlException($"@style must be a well-formed style element: {ex.Message}");
            }
        }

        /// <summary>
        /// The text, without the control characters XML can't hold.
        /// </summary>
        private static string Text(string? value)
        {
            if (value == null)
                return "";

            var clean = new StringBuilder(value.Length);
            var changed = false;
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (char.IsHighSurrogate(c) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                {
                    clean.Append(c).Append(value[++i]);
                }
                else if (XmlConvert.IsXmlChar(c))
                {
                    clean.Append(c);
                }
                else
                {
                    clean.Append('�');
                    changed = true;
                }
            }

            return changed ? clean.ToString() : value;
        }
    }
}
