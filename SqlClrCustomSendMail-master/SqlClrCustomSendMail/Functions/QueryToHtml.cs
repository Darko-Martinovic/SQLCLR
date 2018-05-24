using System;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using SqlClrCustomSendMail;
using System.Data.SqlClient;
using System.Collections.Generic;

public partial class UserDefinedFunctions
{

  
    [SqlFunction(
        Name = "QueryToHtml",
        DataAccess = DataAccessKind.Read,
        SystemDataAccess = SystemDataAccessKind.Read,
        IsDeterministic = true)]
    public static SqlString QueryToHtml
        (
        SqlString query, //query we passed
        [SqlFacet(IsNullable = true, MaxSize = 4000)]SqlString Params,
        [SqlFacet(IsNullable = true, MaxSize = 200)]SqlString caption, //caption 
        [SqlFacet(IsNullable = true, MaxSize = 1000)]SqlString footer, //footer 
        [SqlFacet(IsNullable = true, MaxSize = 4)]SqlInt16 rm, //rotate mode
        [SqlFacet(IsNullable = true, MaxSize = 4)]SqlInt16 rco, //rotate column ordinal
        [SqlFacet(IsNullable = true, MaxSize = 4000)]SqlString style
        )
    {


        var html = "";
        var errorString = "";
        var foo = (RotateMode)Enum.Parse(typeof(RotateMode), rm.ToString());

        var mySp = false;
        var queryValue = query.Value;

        //Make query parametars
        SqlParameter[] listOfParams = null;





        //Dictionary<int, DataTable> ds = DataAccess.GetData(queryValue, mySp, listOfParams, ref html);
        DataAccess da = null;
        var splitQueries = queryValue.Split(';');
        List<object[]> ds = null;
        int counter = 0;

        foreach (var singleQuery in splitQueries)
        {
            if (singleQuery.Trim() == "")
                continue;
            string queryToSend = singleQuery;
            if (singleQuery.ToUpper().StartsWith("EXEC"))
            {
                mySp = true;
                queryToSend = singleQuery.Substring("EXEC".Length, query.Value.Length - "EXEC".Length).Trim();
            }
            da = new DataAccess();
            if (Params.Value.Equals(string.Empty) == false)
                listOfParams = DataAccess.MakeParams(Params.Value, ref html);

            ds = da.GetData(queryToSend, mySp, listOfParams, ref errorString);
            if (errorString.Length == 0)
            {
                var rotate = false;
                var myRco = 0;
                if ((foo == RotateMode.Force) || (foo == RotateMode.Auto && da.RowCount < da.FieldCount))
                {
                    rotate = true;
                    if (rco.IsNull == false)
                        myRco = rco.Value;
                }
                if (rotate == false)
                    html += RenderArray.MakeTable(ds, caption.Value, footer.Value, style.Value, da.FieldCount, da.RowCount, counter, splitQueries.Length - 1);
                else
                {
                    if (myRco == -1)
                        html += RenderArray.MakeTableRotateKeyValue(ds, caption.Value, footer.Value, style.Value, da.FieldCount, da.RowCount, counter, splitQueries.Length - 1);
                    else
                        html += RenderArray.MakeTableRotate(ds, caption.Value, footer.Value, style.Value, da.FieldCount, da.RowCount, myRco, counter, splitQueries.Length - 1);
                }
            }

            else
            {
                html = errorString;
            }
            counter++;
            ds = null;
        }
        try
        {
            ds = null;
            da = null;
            Params = null;
            queryValue = null;
            query = null;
            errorString = null;
            splitQueries = null;
            listOfParams = null;
            caption = null;
            footer = null;
            style = null;
            //GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.Default;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForPendingFinalizers();


        }
        catch (Exception ex)
        {
            html += ex.Message;
        }
        return html;
    }


    public enum RotateMode
    {
        None = 0,
        Auto = 1,
        Force = 2
    }


}
