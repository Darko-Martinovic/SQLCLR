using System;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections.Generic;
using System.Net.Mail;

namespace SqlClrCustomSendMail
{
    class DataAccess
    {
        public readonly List<object[]> Data = new List<object[]>();
        public int RowCount = 0;
        public int FieldCount = 0;
        public List<object[]> GetData(string query, bool isSp, SqlParameter[] listOfParams, ref string html)
        {
            try
            {
                using (var cnn = new SqlConnection("context connection=true"))
                {
                    using (var command = new SqlCommand(query, cnn))
                    {
                        command.Parameters.Clear();
                        cnn.Open();
                        if (isSp)
                            command.CommandType = CommandType.StoredProcedure;
                        if (listOfParams != null)
                        {
                            foreach (var p in listOfParams)
                                command.Parameters.Add(p);
                        }
                        var dr = command.ExecuteReader();

                        FieldCount = dr.FieldCount;
                        var o = new object[FieldCount];
                        for (var i = 0; i < FieldCount; i++)
                            o[i] = dr.GetName(i);

                        Data.Add(o);
                        RowCount++;

                        while (dr.Read())
                        {
                            var o1 = new object[FieldCount];
                            dr.GetValues(o1);
                            Data.Add(o1);
                            RowCount++;
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                html += ex.Message;
            }
            return Data;
        }

        public static SysProfile GetProfile(SqlParameter[] listOfParams, EncryptSupport.Simple3Des wrapper,ref string errorMessage)
        {
            SysProfile p = null;


            try
            {
                using (var cnn = new SqlConnection("context connection=true"))
                {
                    using (var command = new SqlCommand(@"SELECT TOP 1 
                                                                        EnableSsl,
                                                                        DefaultCred,
                                                                        HostName,
                                                                        Port,
                                                                        UserName,
                                                                        Password,
                                                                        Domain,
                                                                        DefaultFrom,
                                                                        DefaultGroup,
                                                                        DeliveryMethod,
                                                                        TimeOut,
                                                                        ProfileName
                                                                FROM EMAIL.PROFILES
                                                                WHERE ProfileName=@name;", cnn))
                    {
                        cnn.Open();
                        if (listOfParams != null)
                        {
                            foreach (var p1 in listOfParams)
                                command.Parameters.Add(p1);
                        }
                        SqlDataReader dr = command.ExecuteReader();
                        if (dr.Read())
                        {
                            p = new SysProfile
                            {
                                Client = new SmtpClient(),
                                Name = (string) dr["ProfileName"]
                            };
                            p.Client.EnableSsl = (bool)dr["EnableSsl"];
                            p.Client.UseDefaultCredentials = (bool)dr["DefaultCred"];
                            p.Client.Host = (string)dr["HostName"];
                            p.Client.Port = (int)dr["Port"];
                            p.Client.Timeout = (int)dr["TimeOut"];
                            p.Client.DeliveryMethod = (SmtpDeliveryMethod)Enum.Parse(typeof(SmtpDeliveryMethod), dr["DeliveryMethod"].ToString());
                            if (dr["Domain"] != DBNull.Value)
                            {
                                p.DefaultDomain = (string)dr["Domain"];
                                if (p.Client.UseDefaultCredentials == false)
                                    p.Client.Credentials =
                                        new System.Net.NetworkCredential(wrapper.DecryptData((string) dr["UserName"]),
                                            wrapper.DecryptData((string) dr["Password"]), p.DefaultDomain);
                                //                                p.client.Credentials = new System.Net.NetworkCredential((string)dr["UserName"], (string)dr["Password"], p.defaultDomain);
                            }
                            else
                                if (p.Client.UseDefaultCredentials == false)
                                    p.Client.Credentials = new System.Net.NetworkCredential(wrapper.DecryptData((string)dr["UserName"]), wrapper.DecryptData((string)dr["Password"]));

                            p.DefaultFromAddress = (string)dr["DefaultFrom"];
                            if (dr["DefaultGroup"] != DBNull.Value)
                                p.DefaultDisplayName = (string)dr["DefaultGroup"];
                            dr.Close();
                        }
                        cnn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                p = null;
                errorMessage = ex.Message ;
            }
            return p;
        }


        public static SysConfig GetConfig(SqlParameter[] listOfParams, ref string errorMessage)
        {
            SysConfig p = null;


            try
            {
                using (SqlConnection cnn = new SqlConnection("context connection=true"))
                {
                    using (SqlCommand command = new SqlCommand(@"SELECT TOP 1 
                                                                        [MaxFileSize]
                                                                       ,[ProhibitedExtensions]
                                                                       ,[LoggingLevel]
                                                                       ,[SaveEmails]
                                                                       ,[SendAsync]
                                                                       ,[NoPiping]
                                                                       ,[SaveAttachments]
                                                                       ,[Name]
                                                                FROM [EMAIL].[Configurations]
                                                                WHERE Name=@name;", cnn))
                    {
                        cnn.Open();
                        if (listOfParams != null)
                        {
                            foreach (var p1 in listOfParams)
                                command.Parameters.Add(p1);
                        }
                        var dr = command.ExecuteReader();
                        if (dr.Read())
                        {
                            p = new SysConfig
                            {
                                MaxFileSize = (Int32) dr["MaxFileSize"],
                                ProhibitedExtension = (string) dr["ProhibitedExtensions"],
                                LoggingLevel =
                                    (ELoggingLevel) Enum.Parse(typeof(ELoggingLevel), dr["LoggingLevel"].ToString()),
                                SaveEmails = (bool) dr["SaveEmails"],
                                NoPiping = (bool) dr["NoPiping"],
                                SaveAttachments = (bool) dr["SaveAttachments"],
                                SendAsync = (bool) dr["SendAsync"],
                                Name = (string) dr["Name"]
                            };
                            dr.Close();
                        }
                        cnn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                p = null;
                errorMessage = ex.Message;
                if ( ex.InnerException != null)
                    errorMessage += ex.InnerException.Message;

            }
            return p;
        }



        public static DataSet GetDataSet(string query, bool isSp, SqlParameter[] listOfParams, string tableMapping,ref string html)
        {
            var ds = new DataSet();
            try
            {
                using (var cnn = new SqlConnection("context connection=true"))
                {
                    using (var command = new SqlCommand(query, cnn))
                    {
                        cnn.Open();
                        if (isSp)
                            command.CommandType = CommandType.StoredProcedure;
                        if (listOfParams != null)
                        {
                            foreach (var p in listOfParams)
                            {
                                command.Parameters.Add(p);
                            }
                        }
                        string[] tm = tableMapping.Split(';');
                        var i = 0;
                        using (var sqlAdp = new SqlDataAdapter())
                        {
                            sqlAdp.SelectCommand = command;
                            foreach (var s in tm)
                            {
                                var addOn = i == 0 ? "" : i.ToString().Trim();
                                sqlAdp.TableMappings.Add("Table" + addOn, s);
                                i++;
                            }
                            sqlAdp.Fill(ds);
                        }
                        cnn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                html += ex.Message;
            }
            return ds;
        }

        public static string GetResult(string query)
        {
            string ds = null;
            try
            {
                using (var cnn = new SqlConnection("context connection=true"))
                {
                    using (var command = new SqlCommand(query, cnn))
                    {
                        cnn.Open();
                        ds = command.ExecuteScalar().ToString();
                        cnn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                ds = ex.Message;
            }
            return ds;
        }

        /// <summary>
        /// https://msdn.microsoft.com/en-us/library/ms131092.aspx
        /// </summary>
        /// <param name="input"></param>
        /// <param name="html"></param>
        /// <returns></returns>
        public static SqlDbType DetermineSqlDbType(string input, ref string html)
        {
            SqlDbType retValue = SqlDbType.NVarChar;
            try
            {

                if (Regex.IsMatch(input.ToLower(), "nvarchar"))  //test it
                    retValue = SqlDbType.NVarChar;
                else if (Regex.IsMatch(input.ToLower(), "int"))  //test it 
                    retValue = SqlDbType.Int;
                else if (Regex.IsMatch(input.ToLower(), "char"))
                    retValue = SqlDbType.Char;
                else if (Regex.IsMatch(input.ToLower(), "decimal")) //test it 
                    retValue = SqlDbType.Decimal;
                else if (Regex.IsMatch(input.ToLower(), "datetime")) // test it
                    retValue = SqlDbType.DateTime;
                else if (Regex.IsMatch(input.ToLower(), "date"))
                    retValue = SqlDbType.Date;
                else if (Regex.IsMatch(input.ToLower(), "bit")) //test it
                    retValue = SqlDbType.Bit;
                else if (Regex.IsMatch(input.ToLower(), "bigint")) // test it
                    retValue = SqlDbType.BigInt;
                else if (Regex.IsMatch(input.ToLower(), "binary"))
                    retValue = SqlDbType.Binary;
                else if (Regex.IsMatch(input.ToLower(), "datetime2"))
                    retValue = SqlDbType.DateTime2;
                else if (Regex.IsMatch(input.ToLower(), "datetimeoffset"))
                    retValue = SqlDbType.DateTimeOffset;
                else if (Regex.IsMatch(input.ToLower(), "float")) // test it
                    retValue = SqlDbType.Float;
                else if (Regex.IsMatch(input.ToLower(), "image"))
                    retValue = SqlDbType.Image;
                else if (Regex.IsMatch(input.ToLower(), "money"))
                    retValue = SqlDbType.Money;
                else if (Regex.IsMatch(input.ToLower(), "nchar")) //test it
                    retValue = SqlDbType.NChar;
                else if (Regex.IsMatch(input.ToLower(), "ntext"))
                    retValue = SqlDbType.NText;
                else if (Regex.IsMatch(input.ToLower(), "real"))
                    retValue = SqlDbType.Real;
                else if (Regex.IsMatch(input.ToLower(), "smalldatetime"))
                    retValue = SqlDbType.SmallDateTime;
                else if (Regex.IsMatch(input.ToLower(), "smallint")) //test it
                    retValue = SqlDbType.SmallInt;
                else if (Regex.IsMatch(input.ToLower(), "smallmoney"))
                    retValue = SqlDbType.SmallMoney;
                else if (Regex.IsMatch(input.ToLower(), "structed"))
                    retValue = SqlDbType.Structured;
                else if (Regex.IsMatch(input.ToLower(), "text"))
                    retValue = SqlDbType.Text;
                else if (Regex.IsMatch(input.ToLower(), "time"))
                    retValue = SqlDbType.Time;
                else if (Regex.IsMatch(input.ToLower(), "timestamp"))
                    retValue = SqlDbType.Timestamp;
                else if (Regex.IsMatch(input.ToLower(), "tinyint"))
                    retValue = SqlDbType.TinyInt;
                else if (Regex.IsMatch(input.ToLower(), "uniqueidentifier")) // test it
                    retValue = SqlDbType.UniqueIdentifier;
                else if (Regex.IsMatch(input.ToLower(), "varbinary"))
                    retValue = SqlDbType.VarBinary;
                else if (Regex.IsMatch(input.ToLower(), "varchar"))
                    retValue = SqlDbType.VarChar;
                else if (Regex.IsMatch(input.ToLower(), "variant"))
                    retValue = SqlDbType.Variant;
                else if (Regex.IsMatch(input.ToLower(), "xml"))
                    retValue = SqlDbType.Xml;



            }
            catch (Exception ex)
            {
                html += ex.Message;
            }

            return retValue;
        }

        public static int DeterminSize(string size, ref byte scale)
        {
            int retValue = 0;
            string[] splitter = size.Split(',');
            if (splitter.Length >= 1)
            {
                var refer = int.TryParse(splitter[0], out var outValue);
                if (refer)
                    retValue = outValue;
            }
            if (splitter.Length == 2)
            {
                var res = byte.TryParse(splitter[1], out var ref1);
                if (res)
                    scale = ref1;
            }
            return retValue;
        }

        public static SqlParameter[] MakeParams(string value, ref string html)
        {
            SqlParameter[] sp = null;
            try
            {
                var splitter = Regex.Replace(value, "\r\n", "", RegexOptions.IgnoreCase).Split(',');
                var pureString = new Dictionary<int, string>();
                for (var i = 0; i < splitter.Length; i++)
                {
                    if (splitter[i].Contains("@") == false && i > 0)
                    {
                        pureString[i - 1] += "," + splitter[i];
                        continue;
                    }
                    pureString.Add(i, splitter[i].Trim());
                }

                var counter = 0;
                foreach (var s in pureString.Values)
                {
                    var s1 = new SqlParameter {ParameterName = s.Trim().Substring(0, s.Trim().IndexOf(' '))};
                    var valueSpliiter = s.Split('=');
                    if (valueSpliiter.Length > 1)
                    {
                        var tester = valueSpliiter[0].Substring(valueSpliiter[0].IndexOf(' '), valueSpliiter[0].Length - valueSpliiter[0].IndexOf(' '));
                        s1.SqlDbType = DetermineSqlDbType(tester, ref html);
                        if (tester.Contains("("))
                        {
                            var pomValue = Regex.Replace(tester, " ", "", RegexOptions.IgnoreCase);
                            var size = pomValue.Substring(pomValue.IndexOf("(", StringComparison.Ordinal) + 1, pomValue.IndexOf(")", StringComparison.Ordinal) - pomValue.IndexOf("(", StringComparison.Ordinal) - 1);
                            byte scale = 0;
                            var sizeTester = DeterminSize(size, ref scale);
                            if (sizeTester != 0)
                                s1.Size = sizeTester;
                            if (scale != 0)
                                s1.Scale = scale;

                        }
                        DetermineValue(valueSpliiter[1], ref s1);

                    }
                    if (sp == null)
                        sp = new SqlParameter[pureString.Values.Count];
                    sp[counter] = s1;
                    counter++;



                }

            }
            catch (Exception ex)
            {
                sp = null;
                html += ex.Message;
            }
            return sp;

        }



        private static void DetermineValue(string valueSplitter, ref SqlParameter s1)
        {
            if (s1.SqlDbType == SqlDbType.Int)
            {
                var succ = int.TryParse(valueSplitter, out var j);
                if (succ)
                    s1.Value = j;
            }
            else if (s1.SqlDbType == SqlDbType.SmallInt)
            {
                var succ = short.TryParse(valueSplitter, out var j);
                if (succ)
                    s1.Value = j;
            }
            else if (s1.SqlDbType == SqlDbType.BigInt)
            {
                var succ = long.TryParse(valueSplitter, out var j);
                if (succ)
                    s1.Value = j;
            }
            else if (s1.SqlDbType == SqlDbType.Real)
            {
                var succ = float.TryParse(valueSplitter, out var j);
                if (succ)
                    s1.Value = j;
            }
            else if (s1.SqlDbType == SqlDbType.Decimal)
            {
                var succ = decimal.TryParse(valueSplitter.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator), out var j);
                if (succ)
                    s1.Value = j;
            }
            else if (s1.SqlDbType == SqlDbType.Float)
            {
                var succ = double.TryParse(valueSplitter.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator), out var j);
                if (succ)
                    s1.Value = j;
            }

            else if (s1.SqlDbType == SqlDbType.Date)
            {
                var valueString = Regex.Replace(valueSplitter, "'", "", RegexOptions.IgnoreCase).Trim();
                s1.Value = DateTime.ParseExact(valueString, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            else if (s1.SqlDbType == SqlDbType.DateTime || s1.SqlDbType == SqlDbType.SmallDateTime)
            {
                var valueString = Regex.Replace(valueSplitter, "'", "", RegexOptions.IgnoreCase).Trim();
                s1.Value = DateTime.ParseExact(valueString, "yyyy-MM-dd hh:mm:ss.fff", CultureInfo.InvariantCulture);
            }
            else if (s1.SqlDbType == SqlDbType.UniqueIdentifier)
            {
                var valueString = Regex.Replace(valueSplitter, "'", "", RegexOptions.IgnoreCase).Trim();
                s1.Value = new Guid(valueString);
            }
            else if (s1.SqlDbType == SqlDbType.Bit) // Bit to boolean
            {
                var valueString = Regex.Replace(valueSplitter, "'", "", RegexOptions.IgnoreCase).Trim();
                var succ = bool.TryParse(valueString, out var result);
                if (succ)
                    s1.Value = result;

            }

            else
            {
                var valueString = valueSplitter.Trim();
                if (valueString.StartsWith("'"))
                    valueString = valueString.Substring(1);

                if (valueString.EndsWith("'"))
                    valueString = valueString.Substring(0, valueString.Length - 1);
                s1.Value = valueString;


            }

        }

        // Method to Execute query







    }


}
