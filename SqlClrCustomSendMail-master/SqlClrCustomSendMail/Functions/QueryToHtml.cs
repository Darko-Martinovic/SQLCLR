using System;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using SqlClrCustomSendMail;
using System.Data.SqlClient;
using System.Collections.Generic;

public partial class UserDefinedFunctions
{

    //[SqlFunction(
    //        Name = "QueryToHtml",
    //        DataAccess = DataAccessKind.Read,
    //        SystemDataAccess = SystemDataAccessKind.Read,
    //        IsDeterministic = true)]
    //public static SqlString QueryToHtml
    //        (
    //        SqlString Query, //query we passed
    //        [SqlFacet(IsNullable = true, MaxSize = 4000)]SqlString Params,
    //        [SqlFacet(IsNullable = true, MaxSize = 200)]SqlString Caption, //caption 
    //        [SqlFacet(IsNullable = true, MaxSize = 1000)]SqlString Footer, //footer 
    //        [SqlFacet(IsNullable = true, MaxSize = 4)]SqlInt16 Rm, //rotate mode
    //        [SqlFacet(IsNullable = true, MaxSize = 4)]SqlInt16 Rco, //rotate column ordinal
    //        [SqlFacet(IsNullable = true, MaxSize = 4000)]SqlString style
    //        )
    //{
    //    SqlString html = QueryToHtml2(Query,Params,Caption,Footer,Rm,Rco,style);
    //    CleanMemory();
    //    return html;
    //}

    [SqlFunction(
        Name = "QueryToHtml",
        DataAccess = DataAccessKind.Read,
        SystemDataAccess = SystemDataAccessKind.Read,
        IsDeterministic = true)]
    public static SqlString QueryToHtml
        (
        SqlString Query, //query we passed
        [SqlFacet(IsNullable = true, MaxSize = 4000)]SqlString Params,
        [SqlFacet(IsNullable = true, MaxSize = 200)]SqlString Caption, //caption 
        [SqlFacet(IsNullable = true, MaxSize = 1000)]SqlString Footer, //footer 
        [SqlFacet(IsNullable = true, MaxSize = 4)]SqlInt16 Rm, //rotate mode
        [SqlFacet(IsNullable = true, MaxSize = 4)]SqlInt16 Rco, //rotate column ordinal
        [SqlFacet(IsNullable = true, MaxSize = 4000)]SqlString style
        )
    {


        string html = "";
        string errorString = "";
        RotateMode foo = (RotateMode)Enum.Parse(typeof(RotateMode), Rm.ToString());

        bool mySp = false;
        string queryValue = Query.Value.ToString();

        //Make query parametars
        SqlParameter[] listOfParams = null;





        //Dictionary<int, DataTable> ds = DataAccess.GetData(queryValue, mySp, listOfParams, ref html);
        DataAccess da = null;
        string[] splitQueries = queryValue.Split(';');
        List<object[]> ds = null;
        int counter = 0;

        foreach (string singleQuery in splitQueries)
        {
            if (singleQuery.Trim() == "")
                continue;
            string queryToSend = singleQuery;
            if (singleQuery.ToUpper().StartsWith("EXEC"))
            {
                mySp = true;
                queryToSend = singleQuery.Substring("EXEC".Length, Query.Value.Length - "EXEC".Length).Trim();
            }
            da = new DataAccess();
            if (Params.Value.ToString().Equals(string.Empty) == false)
                listOfParams = DataAccess.MakeParams(Params.Value, ref html);

            ds = da.GetData(queryToSend, mySp, listOfParams, ref errorString);
            if (errorString.Length == 0)
            {
                bool rotate = false;
                int myRco = 0;
                if ((foo == RotateMode.Force) || (foo == RotateMode.Auto && da.rowCount < da.fieldCount))
                {
                    rotate = true;
                    if (Rco.IsNull == false)
                        myRco = Rco.Value;
                }
                if (rotate == false)
                    html += RenderArray.MakeTable(ds, Caption.Value, Footer.Value, style.Value, da.fieldCount, da.rowCount, counter, splitQueries.Length - 1);
                else
                {
                    if (myRco == -1)
                        html += RenderArray.MakeTableRotateKeyValue(ds, Caption.Value, Footer.Value, style.Value, da.fieldCount, da.rowCount, counter, splitQueries.Length - 1);
                    else
                        html += RenderArray.MakeTableRotate(ds, Caption.Value, Footer.Value, style.Value, da.fieldCount, da.rowCount, myRco, counter, splitQueries.Length - 1);
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
            Query = null;
            errorString = null;
            splitQueries = null;
            listOfParams = null;
            Caption = null;
            Footer = null;
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
