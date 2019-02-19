using System;
using System.Data.SqlTypes;
using System.Xml.Linq;
using Microsoft.SqlServer.Server;
// ReSharper disable RedundantAssignment

// ReSharper disable once UnusedMember.Global
// ReSharper disable once CheckNamespace
public partial class UserDefinedFunctions
{

    [SqlFunction(Name = "ConCatHtml")]
    public static SqlString ConCatHtml
(
        SqlString mHtml, //main html
        SqlString sHtml  //add on html
)
    {
        string retValue = string.Empty;
        try
        {
            var mainHtml = mHtml.Value;
            var addOnHtml = sHtml.Value;
            var styleAddOnName = DetermineStyleName(ExtractStyle(addOnHtml));
            var doc = XDocument.Parse(mainHtml);
            if (styleAddOnName.Equals(string.Empty) == false)
            {
                if (mainHtml.Contains(styleAddOnName) == false)
                {
                    var mainStyle = ExtractStyle(mainHtml);
                    var style = doc.Element("html")?.Element("head")?.Element("style");
                    var addonStyle = ExtractStyle(addOnHtml);
                    style?.Add(addonStyle);
                }
            }
            var body = doc.Element("html")?.Element("body");
            var addOn = ExtractBodyElement(addOnHtml);
            if (body != null)
            {
                body.LastNode.AddAfterSelf(addOn.Descendants("div"));
                retValue = doc.ToString();
                doc = null;
            }

            // ReSharper disable once RedundantAssignment
            body = null;
            // ReSharper disable once RedundantAssignment
            addOn = null;
            mainHtml = null;
            addOnHtml = null;
            styleAddOnName = null;
            mHtml = null;
            sHtml = null;


            //Not allowed! Require change the assembly permission level to UNSAFE
            //GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
            //for (int i = 0; i <= GC.MaxGeneration; i++)
            //{
            //    GC.Collect(i, GCCollectionMode.Forced);

            //}
            GC.WaitForPendingFinalizers();

            //GC.WaitForPendingFinalizers();
            //long memUsed2 = GC.GetTotalMemory(true);
            //if (memUsed2 != memUsed)
            //{
            //    //if (Debugger.IsAttached)
            //    //    Debugger.Break();
            //}
            // Collect all generations of memory.

        }
        catch (Exception ex)
        {
            retValue = ex.Message;
        }
        return retValue;

    }


    private static string ExtractStyle(string tester)
    {
        var result = "";
        var doc = XDocument.Parse(tester);
        var style = doc.Element("html")?.Element("head")?.Element("style");
        if (style != null)
        {
            result = style.Value;
            doc = null;
        }

        style = null;
        return result;

    }

    private static string DetermineStyleName(string style)
    {
        var styleName = "unknowen";

        if (style.Contains("table"))
        {
            var tester = style.Split(new[] { "table" }, StringSplitOptions.RemoveEmptyEntries);
            var valueString = tester[0];
            styleName = valueString.Substring(valueString.IndexOf(".", StringComparison.Ordinal), valueString.Length - valueString.IndexOf(".", StringComparison.Ordinal)).Trim();
        }
        return styleName;

    }

    private static XElement ExtractBodyElement(string tester)
    {
        var doc = XDocument.Parse(tester);
        return doc.Element("html")?.Element("body");
    }


}


