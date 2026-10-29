using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.SqlServer.Server;

namespace SimpleTalk.SqlClr.SendMail
{
    public partial class StoredProcedures
    {
        // Version 1 encrypted the user names and passwords with this secret word, published with its code
        private const string Version1SecretWord = "SimpleTalk";

        /// <summary>
        /// Setup.sql's step from version 1: decrypts the profiles' UserNameV1 and PasswordV1, kept
        /// with version 1's TripleDES key, and saves the password encrypted with the certificate
        /// EmailProfiles instead. Setup.sql creates the procedure, runs it once and drops it.
        /// </summary>
        [SqlProcedure]
        public static void UpgradeVersion1Profiles()
        {
            using (var cnn = new SqlConnection("context connection=true"))
            {
                cnn.Open();
                var profiles = new List<string?[]>();
                using (var command = new SqlCommand("SELECT ProfileName, UserNameV1, PasswordV1 FROM EMAIL.Profiles;", cnn))
                using (var dr = command.ExecuteReader())
                {
                    while (dr.Read())
                        profiles.Add(new[] { ((string)dr[0]).Trim(), dr[1] as string, dr[2] as string });
                }

                foreach (var profile in profiles)
                {
                    string? userName, password;
                    try
                    {
                        userName = profile[1] == null ? null : Decrypt(profile[1]!);
                        password = profile[2] == null ? null : Decrypt(profile[2]!);
                    }
                    catch (Exception ex) when (ex is FormatException || ex is CryptographicException)
                    {
                        PipeUtilities.Print($"The profile '{profile[0]}' has a user name or password that version 1 couldn't decrypt either: set them with EMAIL.SaveProfile.");
                        continue;
                    }

                    using (var command = new SqlCommand(@"
                        UPDATE EMAIL.Profiles
                        SET UserName = @userName,
                            Password = CASE WHEN @password IS NULL THEN NULL ELSE ENCRYPTBYCERT(CERT_ID(N'EmailProfiles'), @password) END
                        WHERE ProfileName = @name;", cnn))
                    {
                        command.Parameters.Add("@name", SqlDbType.Char, 20).Value = profile[0];
                        command.Parameters.Add("@userName", SqlDbType.NVarChar, 200).Value = (object?)userName ?? DBNull.Value;
                        command.Parameters.Add("@password", SqlDbType.NVarChar, 128).Value = (object?)password ?? DBNull.Value;
                        command.ExecuteNonQuery();
                    }

                    PipeUtilities.Print($"The profile '{profile[0]}' now keeps its password encrypted with the certificate EmailProfiles.");
                }
            }
        }

        /// <summary>Version 1's EncryptSupport.Simple3Des.DecryptData.</summary>
        private static string Decrypt(string encrypted)
        {
            using (var tripleDes = new TripleDESCryptoServiceProvider())
            {
                tripleDes.Key = TruncateHash(Version1SecretWord, tripleDes.KeySize / 8);
                tripleDes.IV = TruncateHash("", tripleDes.BlockSize / 8);
                var bytes = Convert.FromBase64String(encrypted);
                using (var ms = new MemoryStream())
                using (var stream = new CryptoStream(ms, tripleDes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.FlushFinalBlock();
                    return Encoding.Unicode.GetString(ms.ToArray());
                }
            }
        }

        private static byte[] TruncateHash(string key, int length)
        {
            using (var sha1 = new SHA1CryptoServiceProvider())
            {
                var hash = sha1.ComputeHash(Encoding.Unicode.GetBytes(key));
                Array.Resize(ref hash, length);
                return hash;
            }
        }
    }
}
