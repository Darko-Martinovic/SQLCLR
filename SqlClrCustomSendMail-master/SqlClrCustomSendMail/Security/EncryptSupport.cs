using System;
using System.Security.Cryptography;

namespace SqlClrCustomSendMail
{
    static class EncryptSupport
    { 
        public sealed class Simple3Des
        {

            private TripleDESCryptoServiceProvider TripleDes = new TripleDESCryptoServiceProvider();
            public Simple3Des(string key)
            {
                TripleDes.Key = TruncateHash(key, TripleDes.KeySize / 8);
                TripleDes.IV = TruncateHash("", TripleDes.BlockSize / 8);
            }

            public string DecryptData(string encryptedtext)
            {


                byte[] encryptedBytes = Convert.FromBase64String(encryptedtext);


                System.IO.MemoryStream ms = new System.IO.MemoryStream();

                CryptoStream decStream = new CryptoStream(ms, TripleDes.CreateDecryptor(), System.Security.Cryptography.CryptoStreamMode.Write);


                decStream.Write(encryptedBytes, 0, encryptedBytes.Length);
                decStream.FlushFinalBlock();


                return System.Text.Encoding.Unicode.GetString(ms.ToArray());
            }


            public string EncryptData(string plaintext)
            {


                byte[] plaintextBytes = System.Text.Encoding.Unicode.GetBytes(plaintext);


                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                CryptoStream encStream = new CryptoStream(ms, TripleDes.CreateEncryptor(), System.Security.Cryptography.CryptoStreamMode.Write);


                encStream.Write(plaintextBytes, 0, plaintextBytes.Length);
                encStream.FlushFinalBlock();


                return Convert.ToBase64String(ms.ToArray());
            }


            private byte[] TruncateHash(string key, int length)
            {

                SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();


                byte[] keyBytes = System.Text.Encoding.Unicode.GetBytes(key);
                byte[] hash = sha1.ComputeHash(keyBytes);


                Array.Resize(ref hash, length);
                return hash;
            }
        }

    }
}
