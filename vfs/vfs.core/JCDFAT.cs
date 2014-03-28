using System.IO;
using vfs.exceptions;

namespace vfs.core
{
    public class JCDFAT : IJCDBasicVFS
    {
        private bool initialized = false;

        // All sizes in this class are given in bytes.
        private const uint numMetaDataBlocks = 1; // Number of blocks used for meta data' (doesn't include the FAT)
        private const uint blockSize = 1 << 12; // 4KB
        private const uint fileEntrySize = 1 << 8; // 256B - should be a power of 2
        private const ulong maxFSSize = (1 << 32) * blockSize + (1 << 32) * (1 << 2);

        private ulong size;
        private uint numBlocks;
        private uint numDataBlocks;
        private uint filesPerBlock;
        private uint fatSize;
        private uint[] fat;

        private JCDFolder rootFolder;
        private JCDFolder currentFolder;

        private FileStream fs;

        private JCDFAT(FileStream fs, ulong size)
        {
            Init(size);
        }

        public static JCDFAT Create(string hfsPath, ulong size)
        {
            // Make sure the directory exists.
            if (File.Exists(Path.GetDirectoryName(hfsPath))) {
                throw new DirectoryNotFoundException();
            }

            // Make sure the file doesn't already exist.
            if (File.Exists(hfsPath)) {
                throw new FileAlreadyExistsException();
            }

            // Create fsfile.
            try {
                var fs = new FileStream(hfsPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
                return new JCDFAT(fs, size);
            } catch (IOException) {
                // The possibly already exists or the stream has been unexpectedly closed
                throw new FileAlreadyExistsException();
            }
        }

        public static JCDFAT Open(string hfsPath)
        {
            FileStream fs;
            try {
                fs = new FileStream(hfsPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            } catch (FileNotFoundException) {
                throw new vfs.core.exceptions.FileNotFoundException();
            }

            // parse meta data to figure out size.
            ulong size = 0L;
            return new JCDFAT(fs, size);
        }

        public void Close()
        {
            this.fs.Flush();
            this.fs.Dispose();
        }

        private void Init(ulong size)
        {
            if (size <= maxFSSize) {
                throw new InvalidSizeException();
            }
            this.size = size;
            this.filesPerBlock = blockSize / fileEntrySize;
            this.numBlocks = (uint)(this.size / blockSize); // Should round down.
            this.fatSize = this.numDataBlocks; // Each block takes up 1 byte.
            this.numDataBlocks = this.numBlocks - numMetaDataBlocks;
            this.fat = new uint[this.numDataBlocks];

            this.initialized = true;
        }

        // Write meta data and FAT to a new FS file.
        // Should only be called on an empty file, and after Init has been called.
        private void InitFSFile(FileStream fs)
        {
            if (!this.initialized) {
                throw new FileSystemNotOpenException();
            }
            // Write meta data and FAT to beginning of fs.
        }

        public ulong Size() { return 0L; }
        public ulong OccupiedSpace() { return 0L; }
        public ulong FreeSpace() { return 0L; }
        public void CreateDirectory(string vfsPath, bool createParents) { return; }
        public void ImportFile(string hfsPath, string vfsPath) { return; }
        public void ExportFile(string vfsPath, string hfsPath) { return; }
        public void DeleteFile(string vfsPath, bool recursive) { return; }
        public void RenameFile(string vfsPath, string newName) { return; }
        public void MoveFile(string vfsPath, string newVfsPath) { return; }
        public JCDFile[] ListDirectory(string vfsPath) { return null; }
        public void SetCurrentDirectory(string vfsPath) { return; }
        public string GetCurrentDirectory() { return null; }
    }
}