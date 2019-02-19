using System;
using System.Data.SqlTypes;

public partial class UserDefinedFunctions
{
    [Microsoft.SqlServer.Server.SqlFunction]
    public static SqlString CleanMemory()
    {
        for (int i = 0; i <= GC.MaxGeneration; i++)
        {
#if NET_4_5
                GC.Collect(i, GCCollectionMode.Forced,true,true);
#elif NET_4_0
                GC.Collect(i, GCCollectionMode.Forced,true);
#elif NET_3_5
            GC.Collect(i, GCCollectionMode.Forced);
#else
            GC.Collect(i, GCCollectionMode.Forced);
#endif


        }
        GC.WaitForPendingFinalizers();
        return new SqlString(string.Empty);
    }
}
