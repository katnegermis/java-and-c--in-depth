using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using vfs.exceptions;

namespace vfs.core
{
    public class JCDVFS : IJCDBasicVFS
    {
        private JCDFAT fat;

        public static JCDVFS Create(string hfsPath, ulong size)
        {
            // Make sure the directory exists.
            if (File.Exists(Path.GetDirectoryName(hfsPath)))
            {
                throw new DirectoryNotFoundException();
            }

            // Make sure the file doesn't already exist.
            if (File.Exists(hfsPath))
            {
                throw new FileAlreadyExistsException();
            }

            if (size >= JCDFAT.globalMaxFSSize)
            {
                Console.Write("Global Max FS Size {0}", JCDFAT.globalMaxFSSize);
                throw new InvalidSizeException();
            }

            // Create fsfile.
            try
            {
                var fs = new FileStream(hfsPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
                return new JCDVFS(fs, size);
            }
            catch (IOException)
            {
                // The file possibly already exists or the stream has been unexpectedly closed
                throw new FileAlreadyExistsException(); //throw not enough space in parent fs
            }
        }

        public static JCDVFS Open(string hfsPath)
        {
            FileStream fs;
            try
            {
                fs = new FileStream(hfsPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            }
            catch (vfs.exceptions.FileNotFoundException)
            {
                throw new vfs.exceptions.FileNotFoundException();
            }

            return new JCDVFS(fs);
        }

        public static void Delete(string hfsPath)
        {
            FileStream fs;
            try
            {
                fs = new FileStream(hfsPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (System.IO.FileNotFoundException)
            {
                throw new vfs.exceptions.FileNotFoundException();
            }
            // Open JCDVFS-file to make sure it actually is a VFS-file. If it is, we delete it.
            var vfs = new JCDVFS(fs);
            vfs.Close();
            File.Delete(hfsPath);
            return;
        }

        /// <summary>
        /// Create a new JCDVFS-file.
        /// </summary>
        /// <param name="fs">Stream to an empty read/write accessible VFS-file.</param>
        /// <param name="size">Maximum size of the new VFS file, in bytes.</param>
        public JCDVFS(FileStream fs, ulong size)
        {
            fat = new JCDFAT(fs, size);
        }

        /// <summary>
        /// Open an existing JCDVFS-file.
        /// </summary>
        /// <param name="fs"></param>
        public JCDVFS(FileStream fs)
        {
            fat = new JCDFAT(fs);
        }

        public void Close()
        {
            fat.Close();
        }

        //Interface methods
        public ulong Size()
        {
            return fat.GetSize();
        }

        public ulong OccupiedSpace()
        {
            return fat.GetSize() - fat.GetFreeSpace();
        }

        public ulong FreeSpace()
        {
            return fat.GetFreeSpace();
        }

        public void CreateDirectory(string vfsPath, bool createParents)
        {
            //Implement relative paths, e.g. fat.Walk(Helpers.PathGetDirName(vfsPath));
            fat.CreateFolder(vfsPath);
        }

        public void ImportFile(string hfsPath, string vfsPath)
        {
            FileStream fileToImport = null;
            if(Directory.Exists(hfsPath)) {
                fat.ImportFolder(hfsPath, vfsPath);
            }
            else {
                try {
                    fileToImport = new FileStream(hfsPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    fat.ImportFile(fileToImport, vfsPath);
                }
                finally {
                    fileToImport.Close();
                }
            }
        }

        public void ExportFile(string vfsPath, string hfsPath)
        {
            fat.ExportFile(vfsPath, hfsPath);
        }

        public void DeleteFile(string vfsPath, bool recursive)
        {
            fat.DeleteFile(vfsPath, recursive);
        }
        public void RenameFile(string vfsPath, string newName)
        {
            fat.RenameFile(vfsPath, newName);
        }
        public void MoveFile(string vfsPath, string newVfsPath)
        {
            fat.MoveFile(vfsPath, newVfsPath);
        }
        public JCDDirEntry[] ListDirectory(string vfsPath)
        {
            var dirEntries = fat.ListDirectory(vfsPath);
            Console.WriteLine("Number of entries: {0}", dirEntries.Length);
            return dirEntries;
        }
        public void SetCurrentDirectory(string vfsPath)
        {
            fat.SetCurrentDirectory(vfsPath);
        }
        public string GetCurrentDirectory()
        {
            return fat.GetCurrentDirectory();
        }

        /// <summary>
        /// Takes the given path and combines it with the CurrentDirectory.
        /// </summary>
        /// <remarks>If the given path is rooted(e.g. absolute), it will be returned unchanged.</remarks>
        /// <param name="path">To combine with the current dir.</param>
        /// <returns>The resulting directory path</returns>
        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.ArgumentNullException"></exception>
        public string CombinePathWithCurrentDirectory(string path)
        {
            if (System.IO.Path.IsPathRooted(path))
                return path;
            else
            {
                var newPath = Path.GetFullPath(Path.Combine(GetCurrentDirectory(), path));
                return newPath;
            }
        }

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
        private static void Compress(Stream input, Stream output)
        {
            using (var compressor = new DeflateStream(output, CompressionMode.Compress))
            {
                input.CopyTo(compressor);
            }
        }

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
        private static void Decompress(Stream input, Stream output)
        {
            using (var decompressor = new DeflateStream(input, CompressionMode.Decompress))
            {
                decompressor.CopyTo(output);
            }
        }

        /// <summary>
        /// Generates a AESCryptoServiceProvider with Key and IV, which can then be used to encrypt
        /// </summary>
        /// <returns>AESCryptoServiceProvider contianing Key and IV</returns>
        private static AesCryptoServiceProvider GenerateAESCryptoServiceProvider()
        {
            AesCryptoServiceProvider AES = new AesCryptoServiceProvider();
            AES.GenerateKey();
            AES.GenerateIV();

            return AES;
        }

        /// <summary>
        /// Encryptes the contents of the input to the output Stream with the given Key and IV
        /// </summary>
        /// <param name="input">Stream to encrypt</param>
        /// <param name="output">Stream to write to</param>
        /// <param name="key">AES Key to use</param>
        /// <param name="iv">AES IV to use</param>
        private static void Encrypt(Stream input, Stream output, byte[] key, byte[] iv)
        {
            AesCryptoServiceProvider AES = new AesCryptoServiceProvider();
            AES.Key = key;
            AES.IV = iv;

            using (var encryptor = new CryptoStream(output, AES.CreateEncryptor(), CryptoStreamMode.Write))
            {
                input.CopyTo(encryptor);
            }
        }

        /// <summary>
        /// Decryptes the contents of the input to the output Stream with the given Key and IV
        /// </summary>
        /// <param name="input">Stream to decrypt from</param>
        /// <param name="output">Stream to write to</param>
        /// <param name="key">AES Key to use</param>
        /// <param name="iv">AES IV to use</param>
        private static void Decrypt(Stream input, Stream output, byte[] key, byte[] iv)
        {
            AesCryptoServiceProvider AES = new AesCryptoServiceProvider();
            AES.Key = key;
            AES.IV = iv;

            using (var decryptor = new CryptoStream(output, AES.CreateDecryptor(), CryptoStreamMode.Write))
            {
                input.CopyTo(decryptor);
            }
        }

        /// <summary>
        /// Compresses and directly also encryptes the input Stream to the output Stream by using the given AES Key and IV
        /// </summary>
        /// <param name="input">Stream to take the data from</param>
        /// <param name="output">Stream to write the data to</param>
        /// <param name="key">AES Key to encrypt with</param>
        /// <param name="iv">AES IV to use for the encryption</param>
        private static void CompressAndEncrypt(Stream input, Stream output, byte[] key, byte[] iv)
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
        }

        /// <summary>
        /// Decrypts and directly decompresses the given input to the output Stream by using the given AES Key and IV
        /// </summary>
        /// <param name="input">Stream to take the data from</param>
        /// <param name="output">Stream to write to</param>
        /// <param name="key">AES Key to decrypt with</param>
        /// <param name="iv">AES IV to use for the decryption</param>
        private static void DecryptAndDecompress(Stream input, Stream output, byte[] key, byte[] iv)
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
        }

    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct ByteToUintConverter
    {
        [FieldOffset(0)]
        public byte[] bytes;

        [FieldOffset(0)]
        public uint[] uints;
    }
}