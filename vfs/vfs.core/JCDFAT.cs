using System.IO;

namespace vfs.core
{
    public class JCDFAT : IJCDBasicVFS
    {
        private bool initialized = false;

        // All sizes in this class are given in bytes.
        private int numMetaDataBlocks = 1; // Number of blocks used for meta data' (doesn't include the FAT)
        private int blockSize = 1 << 12; // 4KB
        private int fileEntrySize = 1 << 8; // 256B - should be a power of 2

        private ulong size;
        private int numBlocks;
        private int numDataBlocks;
        private int filesPerBlock;
        private int fatSize;
        private int[] fat;

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
            // Throws FileNotFoundException.
            var fs = new FileStream(hfsPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            // parse meta data
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
            if (size >= 1 << 46)
            {
                throw new FileTooFuckingBigException();
            }
            this.size = size;
            this.filesPerBlock = this.blockSize / this.fileEntrySize;
            this.numBlocks = this.size / ulong.Parse(this.blockSize); // Should round down.
            this.fatSize = this.numDataBlocks; // Each block takes up 1 byte.
            this.numDataBlocks = this.numBlocks - this.numMetaDataBlocks;
            this.fat = new int[this.numDataBlocks];

            this.initialized = true;
        }

        // Write meta data and FAT to a new FS file.
        // Should only be called on an empty file, and after Init has been called.
        private void InitFSFile(FileStream fs)
        {
            if (!this.initialized) {
                throw new FileSystemNotMounted();
            }
            // Write meta data and FAT to beginning of fs.
        }
    }
}