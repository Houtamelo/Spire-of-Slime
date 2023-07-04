/*using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Save_Management
{
    public static class Encryption
    {
        // This constant is used to determine the keysize of the encryption algorithm in bits.
        // We divide this by 8 within the code below to get the equivalent number of bytes.
        private const int Keysize = 256;

        // This constant determines the number of iterations for the password bytes generation function.
        private const int DerivationIterations = 1000;
        
        private static readonly byte[] SaltStringBytes;
        private static readonly byte[] IvStringBytes;
        private const string PassPhrase = "You_should_not_be_reading_this,_smartass.";

        static Encryption()
        {
            const string saltString = "Don't meddle with the save files, you dork. Seriously, don't. I'm watching you.";
            const string ivString = "This a rogue like, embrace the randomness and accept your fate.";
            
            SaltStringBytes = Encoding.UTF8.GetBytes(saltString).Take(32).ToArray();
            IvStringBytes = Encoding.UTF8.GetBytes(ivString).Take(32).ToArray();
            
            Debug.Assert(SaltStringBytes.Length == 32);
            Debug.Assert(IvStringBytes.Length == 32);
        }

        public static string Encrypt(string plainText)
        {
            // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
            // so that the same Salt and IV values can be used when decrypting.  
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            using (Rfc2898DeriveBytes password = new(PassPhrase, SaltStringBytes, DerivationIterations))
            {
                byte[] keyBytes = password.GetBytes(Keysize / 8);
                using (RijndaelManaged symmetricKey = new())
                {
                    symmetricKey.BlockSize = 256;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, IvStringBytes))
                    {
                        using (MemoryStream memoryStream = new())
                        {
                            using (CryptoStream cryptoStream = new(memoryStream, encryptor, CryptoStreamMode.Write))
                            {
                                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                cryptoStream.FlushFinalBlock();
                                // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
                                byte[] cipherTextBytes = SaltStringBytes;
                                cipherTextBytes = cipherTextBytes.Concat(IvStringBytes).ToArray();
                                cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                                memoryStream.Close();
                                cryptoStream.Close();
                                return Convert.ToBase64String(cipherTextBytes);
                            }
                        }
                    }
                }
            }
        }

        public static string Decrypt(string cipherText)
        {
            // Get the complete stream of bytes that represent:
            // [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
            byte[] cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
            // Get the actual cipher text bytes by removing the first 64 bytes from the cipherText string.
            byte[] cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip((Keysize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((Keysize / 8) * 2)).ToArray();

            using (Rfc2898DeriveBytes password = new(PassPhrase, SaltStringBytes, DerivationIterations))
            {
                byte[] keyBytes = password.GetBytes(Keysize / 8);
                using (RijndaelManaged symmetricKey = new())
                {
                    symmetricKey.BlockSize = 256;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, IvStringBytes))
                    {
                        using (MemoryStream memoryStream = new(cipherTextBytes))
                        {
                            using (CryptoStream cryptoStream = new(memoryStream, decryptor, CryptoStreamMode.Read))
                            using (StreamReader streamReader = new(cryptoStream, Encoding.UTF8))
                            {
                                return streamReader.ReadToEnd();
                            }
                        }
                    }
                }
            }
        }
    }
}*/