using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Collections;
using Microsoft.SqlServer.Server;
using System.Linq;
using System.Runtime;
using System.Diagnostics;

public partial class UserDefinedFunctions
{

    [SqlFunction(Name = "ConCatHtml")]
    public static SqlString ConCatHtml
(
        SqlString mHtml, //main html
        SqlString sHtml  //add on html
)
    {
        XDocument doc = null;
        string retValue = string.Empty;
        try
        {
            string mainHtml = mHtml.Value;
            string addOnHtml = sHtml.Value;
            string styleAddOnName = DetermineStyleName(ExtractStyle(addOnHtml));
            doc = XDocument.Parse(mainHtml);
            if (styleAddOnName.Equals(string.Empty) == false)
            {
                if (mainHtml.Contains(styleAddOnName) == false)
                {
                    string mainStyle = ExtractStyle(mainHtml);
                    XElement style = doc.Element("html").Element("head").Element("style");
                    string addonStyle = ExtractStyle(addOnHtml);
                    style.Add(addonStyle);
                }
            }
            XElement body = doc.Element("html").Element("body");
            XElement addON = ExtractBodyElement(addOnHtml);
            body.LastNode.AddAfterSelf(addON.Descendants("div"));
            retValue = doc.ToString();
            doc = null;
            body = null;
            addON = null;
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


    public static string ExtractStyle(string tester)
    {
        string result = "";
        XDocument doc = XDocument.Parse(tester);
        XElement style = doc.Element("html").Element("head").Element("style");
        result = style.Value;
        doc = null;
        style = null;
        return result;

    }
    public static string DetermineStyleName(string style)
    {
        string styleName = "unknowen";
        string[] tester = null;

        if (style.Contains("table"))
        {
            tester = style.Split(new string[] { "table" }, StringSplitOptions.RemoveEmptyEntries);
            string valueString = tester[0];
            styleName = valueString.Substring(valueString.IndexOf("."), valueString.Length - valueString.IndexOf(".")).Trim();
        }
        return styleName;

    }
    public static XElement ExtractBodyElement(string tester)
    {
        XDocument doc = XDocument.Parse(tester);
        return doc.Element("html").Element("body");
    }


}


