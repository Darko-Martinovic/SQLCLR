using System;
using System.Security.Cryptography;

// ReSharper disable once CheckNamespace
namespace SqlClrCustomSendMail
{
    static class EncryptSupport
    { 
        public sealed class Simple3Des
        {

            private readonly TripleDESCryptoServiceProvider _tripleDes = new TripleDESCryptoServiceProvider();
            public Simple3Des(string key)
            {
                _tripleDes.Key = TruncateHash(key, _tripleDes.KeySize / 8);
                _tripleDes.IV = TruncateHash("", _tripleDes.BlockSize / 8);
            }

            public string DecryptData(string encryptedtext)
            {


                var encryptedBytes = Convert.FromBase64String(encryptedtext);


                var ms = new System.IO.MemoryStream();

                var decStream = new CryptoStream(ms, _tripleDes.CreateDecryptor(), CryptoStreamMode.Write);


                decStream.Write(encryptedBytes, 0, encryptedBytes.Length);
                decStream.FlushFinalBlock();


                return System.Text.Encoding.Unicode.GetString(ms.ToArray());
            }


            public string EncryptData(string plaintext)
            {


                var plaintextBytes = System.Text.Encoding.Unicode.GetBytes(plaintext);


                var ms = new System.IO.MemoryStream();
                var encStream = new CryptoStream(ms, _tripleDes.CreateEncryptor(), CryptoStreamMode.Write);


                encStream.Write(plaintextBytes, 0, plaintextBytes.Length);
                encStream.FlushFinalBlock();


                return Convert.ToBase64String(ms.ToArray());
            }


            private byte[] TruncateHash(string key, int length)
            {

                var sha1 = new SHA1CryptoServiceProvider();


                var keyBytes = System.Text.Encoding.Unicode.GetBytes(key);
                var hash = sha1.ComputeHash(keyBytes);


                Array.Resize(ref hash, length);
                return hash;
            }
        }

    }
}
