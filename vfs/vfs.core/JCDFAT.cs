using System.IO;

namespace vfs.core
{
    public class JCDFAT : IBasicVFS
    {
        private bool initialized = false;
        private bool mounted = false;

        // All sizes in this class are given in bytes.
        private int numMetaDataBlocks = 1; // Number of blocks used for meta data' (doesn't include the FAT)
        private int blockSize = 1 << 12; // 4KB
        private int fileEntrySize = 1 << 8; // 256B - should be a power of 2
        private int filesPerBlock = blockSize / fileEntrySize;

        private ulong size;
        private int numBlocks;
        private int numDataBlocks;
        private int fatSize;
        private int[] fat;

        private JCDFolder rootFolder;
        private JCDFolder currentFolder;

        private FileStream fs;

        public static void Create(string hfsPath, ulong size)
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
                Init(size);
            } catch (IOException) {
                // The possibly already exists or the stream has been unexpectedly closed
                throw new FileAlreadyExistsException();
            }
        }

        public void Mount(string hfsPath)
        {
            // Throws FileNotFoundException.
            this.fs = new FileStream(hfsPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Mount(this.fs);
        }

        private void Mount(FileStream fs)
        {
            // Parse meta data from fs.
            // Init(size);
            // Set this.rootDirectory and this.currentDirectory.
            // Initialize this.fat. At least read in root directory block.
            this.mounted = true;
        }

        public void Unmount()
        {
            if (!this.mounted) {
                throw new FileSystemNotMounted();
            }

            this.fs.Flush();
            this.fs.Dispose();
        }

        private void Init(ulong size)
        {
            this.size = size;
            this.numBlocks = this.size / this.blockSize; // Should round down.
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
                throw new Exception("Init not called yet!");
            }
            // Write meta data and FAT to beginning of fs.
        }
    }
}