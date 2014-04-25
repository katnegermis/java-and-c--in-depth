using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace vfs.core
{
    public class Helpers
    {
        /// <summary>
        /// Round up integer divison
        /// </summary>
        /// <param name="num">Numerator</param>
        /// <param name="den">Denominator</param>
        /// <returns></returns>
        public static ulong ruid(ulong num, ulong den)
        {
            return (num + den - 1) / den;
        }

        public static long ruid(long num, long den) {
            return (num + den - 1) / den;
        }

        public static int ruid(int num, int den) {
            return (num + den - 1) / den;
        }

        public static uint ruid(uint num, uint den) {
            return (num + den - 1) / den;
        }

        public static string PathCombine(string path, string fileName)
        {
            return System.IO.Path.Combine(path, fileName);
        }

        public static string PathGetDirectoryName(string path)
        {
            var tmp = TrimLastSlash(path);
            var slash = tmp.LastIndexOf('/');
            if(slash > -1) {
                tmp = tmp.Remove(slash + 1);
            }
            else {
                return ".";
            }

            return tmp;
        }

        public static string PathGetFileName(string path)
        {
            //return System.IO.Path.GetFileName(path);
            var tmp = TrimLastSlash(path);
            return tmp.Substring(tmp.LastIndexOf("/") + 1);
        }

        /*public static bool PathIsValid(string path) {
            return true;
        }
        public static bool PathIsValid(string path, bool isFolder) {
            return true;
        }*/

        public static string TrimLastSlash(string name) {
            return name.TrimEnd(new char[] { '/' });
        }

        public static bool FileNameIsValid(string name) {
            return (name != "." && name != ".." && name.IndexOf('/') < 0);
        }

        internal delegate JCDFile CreateHiddenFileDelegate(string path, uint firstBlock);

        /// <summary>
        /// Method to compress the input Stream and write the result into the output Stream 
        /// </summary>
        /// <param name="input">Stream where the bytes to compress come from</param>
        /// <param name="output">Stream where the compressed bytes go to</param>
        /// <example>
        /// using (FileStream input = File.Open(@"C:\source.txt", FileMode.Open))
        ///    using (FileStream fileStream = File.Create(@"C:\temp.txt"))
        ///    { 
        ///        Compress(input, fileStream);
        ///    }
        /// </example>
       /* private static void Compress(Stream input, Stream output)
        {
            using (var compressor = new DeflateStream(output, CompressionMode.Compress))
            {
                input.CopyTo(compressor);
            }
        }*/

        /// <summary>
        /// Method to decompress the input Stream and write the result into the output Stream
        /// </summary>
        /// <param name="input">Stream where the bytes to decompress come from</param>
        /// <param name="output">Stream where the decompressed bytes go to</param>
        /// <example>
        /// using (FileStream input = File.Open(@"C:\temp.txt", FileMode.Open))
        ///    using (FileStream fileStream = File.Create(@"C:\target.txt"))
        ///    {
        ///        Decompress(input, fileStream);
        ///    }
        /// </example>
      /*  private static void Decompress(Stream input, Stream output)
        {
            using (var decompressor = new DeflateStream(input, CompressionMode.Decompress))
            {
                decompressor.CopyTo(output);
            }
        }*/

        /// <summary>
        /// Generates a AESCryptoServiceProvider with Key and IV, which can then be used to encrypt
        /// </summary>
        /// <returns>AESCryptoServiceProvider contianing Key and IV</returns>
       /* private static AesCryptoServiceProvider GenerateAESCryptoServiceProvider()
        {
            AesCryptoServiceProvider AES = new AesCryptoServiceProvider();
            AES.GenerateKey();
            AES.GenerateIV();

            return AES;
        }*/

        /// <summary>
        /// Encryptes the contents of the input to the output Stream with the given Key and IV
        /// </summary>
        /// <param name="input">Stream to encrypt</param>
        /// <param name="output">Stream to write to</param>
        /// <param name="key">AES Key to use</param>
        /// <param name="iv">AES IV to use</param>
      /*  private static void Encrypt(Stream input, Stream output, byte[] key, byte[] iv)
        {
            AesCryptoServiceProvider AES = new AesCryptoServiceProvider();
            AES.Key = key;
            AES.IV = iv;

            using (var encryptor = new CryptoStream(output, AES.CreateEncryptor(), CryptoStreamMode.Write))
            {
                input.CopyTo(encryptor);
            }
        }*/

        /// <summary>
        /// Decryptes the contents of the input to the output Stream with the given Key and IV
        /// </summary>
        /// <param name="input">Stream to decrypt from</param>
        /// <param name="output">Stream to write to</param>
        /// <param name="key">AES Key to use</param>
        /// <param name="iv">AES IV to use</param>
       /* private static void Decrypt(Stream input, Stream output, byte[] key, byte[] iv)
        {
            AesCryptoServiceProvider AES = new AesCryptoServiceProvider();
            AES.Key = key;
            AES.IV = iv;

            using (var decryptor = new CryptoStream(output, AES.CreateDecryptor(), CryptoStreamMode.Write))
            {
                input.CopyTo(decryptor);
            }
        }*/

        /// <summary>
        /// Compresses and directly also encryptes the input Stream to the output Stream by using the given AES Key and IV
        /// </summary>
        /// <param name="input">Stream to take the data from</param>
        /// <param name="output">Stream to write the data to</param>
        /// <param name="key">AES Key to encrypt with</param>
        /// <param name="iv">AES IV to use for the encryption</param>
       /* private static void CompressAndEncrypt(Stream input, Stream output, byte[] key, byte[] iv)
        {
            AesCryptoServiceProvider AES = new AesCryptoServiceProvider();
            AES.Key = key;
            AES.IV = iv;

            using (var encryptor = new CryptoStream(output, AES.CreateEncryptor(), CryptoStreamMode.Write))
            {
                using (var compressor = new DeflateStream(encryptor, CompressionMode.Compress))
                {
                    input.CopyTo(compressor);
                }
            }
        }*/

        /// <summary>
        /// Decrypts and directly decompresses the given input to the output Stream by using the given AES Key and IV
        /// </summary>
        /// <param name="input">Stream to take the data from</param>
        /// <param name="output">Stream to write to</param>
        /// <param name="key">AES Key to decrypt with</param>
        /// <param name="iv">AES IV to use for the decryption</param>
        /*private static void DecryptAndDecompress(Stream input, Stream output, byte[] key, byte[] iv)
        {
            AesCryptoServiceProvider AES = new AesCryptoServiceProvider();
            AES.Key = key;
            AES.IV = iv;

            using (var decryptor = new CryptoStream(input, AES.CreateDecryptor(), CryptoStreamMode.Read))
            {
                using (var decompressor = new DeflateStream(decryptor, CompressionMode.Decompress))
                {
                    decompressor.CopyTo(output);
                }
            }
        }*/
    }
}
