using System;
using System.IO;
using System.Runtime.InteropServices;
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
            catch (FileNotFoundException)
            {
                throw new FileNotFoundException();
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
            catch (FileNotFoundException)
            {
                throw new FileNotFoundException();
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
            return fat.getSize();
        }
        public ulong OccupiedSpace()
        {
            return fat.getSize() - fat.getFreeSpace();
        }
        public ulong FreeSpace()
        {
            return fat.getFreeSpace();
        }
        public void CreateDirectory(string vfsPath, bool createParents)
        {
            return;
        }
        public void ImportFile(string hfsPath, string vfsPath)
        {
            return;
        }
        public void ExportFile(string vfsPath, string hfsPath)
        {
            return;
        }
        public void DeleteFile(string vfsPath, bool recursive)
        {
            return;
        }
        public void RenameFile(string vfsPath, string newName)
        {
            return;
        }
        public void MoveFile(string vfsPath, string newVfsPath)
        {
            return;
        }
        public JCDDirEntry[] ListDirectory(string vfsPath)
        {
            return null;
        }
        public void SetCurrentDirectory(string vfsPath)
        {
            return;
        }
        public string GetCurrentDirectory()
        {
            return null;
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