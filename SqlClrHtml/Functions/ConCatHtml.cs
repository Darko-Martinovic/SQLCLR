using System.Data.SqlTypes;
using System.Xml;
using System.Xml.Linq;
using Microsoft.SqlServer.Server;

namespace SimpleTalk.SqlClr.Html
{
    public partial class UserDefinedFunctions
    {
        /// <summary>
        /// Two documents of QueryToHtml as one: the second one's tables after the first one's, and
        /// its style sheet in the head unless the first one has it already.
        /// </summary>
        [SqlFunction(Name = "ConCatHtml", DataAccess = DataAccessKind.None, IsDeterministic = true)]
        public static SqlString ConCatHtml
            (
            SqlString mHtml,  // main html
            SqlString sHtml   // add on html
            )
        {
            if (mHtml.IsNull)
                return sHtml;
            if (sHtml.IsNull)
                return mHtml;

            var main = Parse(mHtml.Value, "@mHtml");
            var addOn = Parse(sHtml.Value, "@sHtml");

            var head = main.Element("head");
            if (head == null)
            {
                head = new XElement("head");
                main.AddFirst(head);
            }

            var addOnHead = addOn.Element("head");
            if (addOnHead != null)
            {
                foreach (var style in addOnHead.Elements("style"))
                {
                    var present = false;
                    foreach (var existing in head.Elements("style"))
                        present |= existing.Value == style.Value;
                    if (!present)
                        head.Add(new XElement(style));
                }
            }

            var body = main.Element("body")!;
            foreach (var node in addOn.Element("body")!.Nodes())
                body.Add(node);

            return HtmlDocument.Write(main);
        }

        private static XElement Parse(string html, string parameter)
        {
            XElement document;
            try
            {
                document = XElement.Parse(html);
            }
            catch (XmlException ex)
            {
                throw new HtmlException($"{parameter} is not HTML from QueryToHtml or ConCatHtml: {ex.Message}");
            }

            if (document.Name.LocalName != "html" || document.Element("body") == null)
                throw new HtmlException($"{parameter} is not HTML from QueryToHtml or ConCatHtml: it needs an html element with a body.");
            return document;
        }
    }
}
