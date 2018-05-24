using System;
using System.Security.Cryptography;

namespace SqlClrCustomSendMail
{
    static class EncryptSupport
    { 
        public sealed class Simple3Des
        {

            private TripleDESCryptoServiceProvider _tripleDes = new TripleDESCryptoServiceProvider();
            public Simple3Des(string key)
            {
                _tripleDes.Key = TruncateHash(key, _tripleDes.KeySize / 8);
                _tripleDes.IV = TruncateHash("", _tripleDes.BlockSize / 8);
            }

            public string DecryptData(string encryptedtext)
            {


                byte[] encryptedBytes = Convert.FromBase64String(encryptedtext);


                System.IO.MemoryStream ms = new System.IO.MemoryStream();

                CryptoStream decStream = new CryptoStream(ms, _tripleDes.CreateDecryptor(), System.Security.Cryptography.CryptoStreamMode.Write);


                decStream.Write(encryptedBytes, 0, encryptedBytes.Length);
                decStream.FlushFinalBlock();


                return System.Text.Encoding.Unicode.GetString(ms.ToArray());
            }


            public string EncryptData(string plaintext)
            {


                byte[] plaintextBytes = System.Text.Encoding.Unicode.GetBytes(plaintext);


                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                CryptoStream encStream = new CryptoStream(ms, _tripleDes.CreateEncryptor(), System.Security.Cryptography.CryptoStreamMode.Write);


                encStream.Write(plaintextBytes, 0, plaintextBytes.Length);
                encStream.FlushFinalBlock();


                return Convert.ToBase64String(ms.ToArray());
            }


            private byte[] TruncateHash(string key, int length)
            {

                var sha1 = new SHA1CryptoServiceProvider();


                byte[] keyBytes = System.Text.Encoding.Unicode.GetBytes(key);
                byte[] hash = sha1.ComputeHash(keyBytes);


                Array.Resize(ref hash, length);
                return hash;
            }
        }

    }
}
