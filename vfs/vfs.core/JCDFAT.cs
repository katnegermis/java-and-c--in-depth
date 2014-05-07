using System;
using System.Linq;
using System.IO;
using vfs.exceptions;
using vfs.core.visitor;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using vfs.core.indexing;
using vfs.common;

namespace vfs.core
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct ByteToUintConverter {
        [FieldOffset(0)]
        public byte[] bytes;

        [FieldOffset(0)]
        public uint[] uints;
    }

    internal delegate JCDFile CreateHiddenFileDelegate(string path, uint firstBlock);

    public class JCDFAT : IJCDBasicVFS, IDisposable
    {
        internal FileIndex fileIndex;
        private const uint magicNumber = 0x13371337;
        private const uint freeBlock = 0xFFFFFFFF;
        private const uint endOfChain = 0xFFFFFFFE;

        internal const uint rootDirBlock = 0;
        internal const uint searchFileTreeBlock = 1;
        internal const uint searchFileDataBlock = 2;

        internal string searchFileTreePath = "/searchfiletree";
        internal string searchFileDataPath = "/searchfiledata";

        private const uint readBufferSize = 40 * 1024; //In blocks

        // All sizes in this class are given in bytes unless otherwise specified.
        internal const uint reservedBlockNumbers = 2; // End-of-chain and free
        // See this stackoverflow answer for bit shifting behaviour in c#: http://stackoverflow.com/questions/9210373/why-do-shift-operations-always-result-in-a-signed-int-when-operand-is-32-bits
        private const uint availableBlockNumbers = (uint)((1L << 32) - reservedBlockNumbers); // 32-bit block numbers
        private const uint metaDataBlocks = 1; // Number of blocks used for meta data (doesn't include the FAT)
        internal const uint blockSize = 1 << 12; // 4KB blocks
        internal const ulong globalMaxFSSize = (ulong)availableBlockNumbers * blockSize + (1L << 32) * 4 + metaDataBlocks * blockSize;
        //numBlocks * (blockSize + fatEntrySize) + metaDataSize. FAT size is rounded up to a whole number of blocks, assuming reservedBlockNumbers < blockSize/4.
        internal const uint dirEntrySize = 1 << 8; // 256B == JCDDirEntry.StructSize();
        internal const uint fatEntriesPerBlock = blockSize / 4;
        internal const uint dirEntriesPerBlock = blockSize / dirEntrySize;

        private const int freeBlocksOffset = 12;
        private const int firstFreeBlockOffset = 16;

        //currentSize, currentNumBlocks, currentNumDataBlocks are currently not updated! Do not use them!
        private ulong currentSize, maxSize;
        private ulong currentNumBlocks; // Can actually exceed a uint!
        private uint maxNumBlocks;
        private uint currentNumDataBlocks, maxNumDataBlocks;
        private uint fatBlocks;
        private uint dataOffsetBlocks;
        private uint freeBlocks;
        private uint firstFreeBlock;
        private uint[] fat;

        private JCDFolder rootFolder;
        private JCDFolder currentFolder;

        private FileStream hfsFileStream;
        private BinaryWriter hfsBinaryWriter;
        private BinaryReader hfsBinaryReader;

        /// <summary>
        /// Triggered whenever a file has been added to the file system.
        /// </summary>
        public event AddFileEventHandler FileAdded;

        /// <summary>
        /// Triggered whenever a file has been deleted on the file system.
        /// </summary>
        public event DeleteFileEventHandler FileDeleted;

        /// <summary>
        /// Triggered whenever a file within the file system is moved or renamed.
        /// </summary>
        public event MoveFileEventHandler FileMoved;

        /// <summary>
        /// Triggered whenever the data of a file within the file system is modified.
        /// This does NOT include renaming, moving, or deleting.
        /// </summary>
        public event ModifyFileEventHandler FileModified;

        /// <summary>
        /// Triggered whenever a file (NOT a folder) is resized.
        /// </summary>
        public event ResizeFileEventHandler FileResized;

        /// <summary>
        /// Event to be called every time a new file is added to the file system.
        /// </summary>
        /// <param name="path">Path of the newly added file.</param>
        internal void OnFileAdded(string path) {
            if (FileAdded != null) {
                FileAdded(path);
            }
        }

        /// <summary>
        /// Event to be called every time a file is deleted from the file system.
        /// </summary>
        /// <param name="path">Path of the deleted file.</param>
        internal void OnFileDeleted(string path) {
            if (FileDeleted != null) {
                FileDeleted(path);
            }
        }

        /// <summary>
        /// Event to be called every time a file is moved or renamed on the file system.
        /// </summary>
        /// <param name="oldPath">File's previous (old) path.</param>
        /// <param name="newPath">File's new (current) path.</param>
        internal void OnFileMoved(string oldPath, string newPath) {
            if (FileMoved != null) {
                FileMoved(oldPath, newPath);
            }
        }

        /// <summary>
        /// Event to be called every time a file is modified.
        /// This does NOT include file resizing!
        /// </summary>
        /// <param name="path">File's path.</param>
        /// <param name="offset">Offset in to file where the data was written.</param>
        /// <param name="data">Data that was written.</param>
        internal void OnFileModified(string path, long offset, byte[] data) {
            // Don't track internal files.
            if (path == searchFileDataPath || path == searchFileTreePath) {
                return;
            }

            if (FileModified != null) {
                FileModified(path, offset, data);
            }
        }

        /// <summary>
        /// Event to be called every time a file (NOT folder) is resized.
        /// </summary>
        /// <param name="path">Path to the file.</param>
        /// <param name="newSize">New size of the file.</param>
        internal void OnFileResized(string path, long newSize) {
            if (FileResized != null) {
                FileResized(path, newSize);
            }
        }

        public static JCDFAT Create(string hfsPath, ulong size) {
            // Make sure the directory exists.
            if(File.Exists(Path.GetDirectoryName(hfsPath))) {
                throw new DirectoryNotFoundException();
            }

            // Make sure the file doesn't already exist.
            if(File.Exists(hfsPath)) {
                throw new FileAlreadyExistsException();
            }

            if(size >= JCDFAT.globalMaxFSSize) {
                Console.Write("Global Max FS Size {0}", JCDFAT.globalMaxFSSize);
                throw new InvalidSizeException();
            }

            // Create fsfile.
            try {
                var fs = new FileStream(hfsPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
                return new JCDFAT(fs, size);
            }
            catch(IOException) {
                // The file possibly already exists or the stream has been unexpectedly closed
                throw new FileAlreadyExistsException(); //throw not enough space in parent fs
            }
        }

        public static JCDFAT Open(string hfsPath) {
            FileStream fs;
            try {
                fs = new FileStream(hfsPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            }
            catch(System.IO.FileNotFoundException) {
                throw new vfs.exceptions.FileNotFoundException();
            }

            return new JCDFAT(fs);
        }

        public static void Delete(string hfsPath) {
            FileStream fs;
            try {
                fs = new FileStream(hfsPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch(System.IO.FileNotFoundException) {
                throw new vfs.exceptions.FileNotFoundException();
            }
            // Open JCDVFS-file to make sure it actually is a VFS-file. If it is, we delete it.
            var vfs = new JCDFAT(fs);
            vfs.Close();
            File.Delete(hfsPath);
            return;
        }


        /// <summary>
        /// Create a new JCDFAT-file.
        /// </summary>
        /// <param name="fs">Stream to a file open with read/write access.</param>
        /// <param name="size">Maximum size of the new JCDVFS-file.</param>
        private JCDFAT(FileStream fs, ulong size)
        {
            this.hfsFileStream = fs;

            // TODO: Make sure that the file is empty??

            NewFSSetSize(size);

            hfsBinaryWriter = new BinaryWriter(fs);
            hfsBinaryReader = new BinaryReader(fs);

            NewFSWriteMetaData();
            NewFSInitAndWriteFAT();
            NewFSCreateRootFolder();
            NewFSCreateSearchFile();
            // Make sure that the file system is written to disk.
            hfsBinaryWriter.Flush();
        }

        /// <summary>
        /// Open an existing JCDFAT-file.
        /// </summary>
        /// <param name="fs">Stream to a JCDVFS-file, open with read/write access.</param>
        private JCDFAT(FileStream fs)
        {
            this.hfsFileStream = fs;

            hfsBinaryWriter = new BinaryWriter(fs);
            hfsBinaryReader = new BinaryReader(fs);

            ParseMetaData();
            InitSize(false);
            ReadFAT();
            InitRootFolder();
            InitSearchFile();

            /*var rootDir = (BlockCounterVisitor)WalkFATChain(rootDirBlock, new BlockCounterVisitor());
            Console.WriteLine("Root dir spans {0} blocks", rootDir.Blocks);
            var searchFile = (BlockCounterVisitor)WalkFATChain(searchFileBlock, new BlockCounterVisitor());
            Console.WriteLine("Search file spans {0} blocks", searchFile.Blocks);*/
        }

        /// <summary>
        /// Update the meta-data field "free blocks".
        /// </summary>
        /// <param name="newVal">New number of free blocks.</param>
        private void SetFreeBlocks(uint newVal)
        {
            freeBlocks = newVal;
            Write(freeBlocksOffset, freeBlocks);
        }

        /// <summary>
        /// Update the firstFreeBlock variable and the associated meta-data field.
        /// </summary>
        /// <param name="newVal"></param>
        private void SetFirstFreeBlock(uint newVal)
        {
            firstFreeBlock = newVal;
            Write(firstFreeBlockOffset, firstFreeBlock);
        }

        /// <summary>
        /// Write data to JCDVFS-file.
        /// </summary>
        /// <param name="offset">Absolute offset in to JCDVFS-file at which to start writing data.</param>
        /// <param name="data">The buffer containing data to write to JCDVFS-file.</param>
        /// <param name="arrOffset">The zero-based byte offset in data from which to begin copying bytes to JCDVFS-file.</param>
        /// <param name="count">The maximum number of bytes to write.</param>
        internal void Write(ulong offset, byte[] data, int arrOffset, int count)
        {
            Seek(offset);
            hfsFileStream.Write(data, arrOffset, count);
        }

        /// <summary>
        /// Write data to JCDVFS-file.
        /// </summary>
        /// <param name="offset">Absolute offset in to JCDVFS-file at which to start writing data.</param>
        /// <param name="data">Data to write to JCDVFS-file.</param>
        internal void Write(ulong offset, byte[] data)
        {
            Seek(offset);
            hfsBinaryWriter.Write(data);
        }

        /// <summary>
        /// Write data to JCDVFS-file.
        /// </summary>
        /// <param name="offset">Absolute offset in to JCDVFS-file at which to start writing data.</param>
        /// <param name="data">Data to write to JCDVFS-file.</param>
        internal void Write(ulong offset, ushort data)
        {
            Seek(offset);
            hfsBinaryWriter.Write(data);
        }

        /// <summary>
        /// Write data to JCDVFS-file.
        /// </summary>
        /// <param name="offset">Absolute offset in to JCDVFS-file at which to start writing data.</param>
        /// <param name="data">Data to write to JCDVFS-file.</param>
        internal void Write(ulong offset, uint data)
        {
            Seek(offset);
            hfsBinaryWriter.Write(data);
        }

        private void Seek(ulong offset)
        {
            if (hfsFileStream.Position == (long)offset)
            {
                return;
            }
            hfsFileStream.Seek((long)offset, SeekOrigin.Begin);
        }

        /// <summary>
        /// Read raw data from the JCDVFS-file.
        /// </summary>
        /// <param name="offset">Byte-offset of point to start reading from.</param>
        /// <param name="length">Length in bytes.</param>
        /// <returns>Byte array of length `length`.</returns>
        internal byte[] Read(ulong offset, uint length)
        {
            Seek(offset);
            return hfsBinaryReader.ReadBytes((int)length);
        }

        /// <summary>
        /// Get the byte offset in the JCDVFS-file in which the nth index of the FAT is positioned.
        /// </summary>
        /// <param name="index">Index in to the FAT.</param>
        /// <returns>Byte offset in to JCDVFS-file of the nth index of the FAT.</returns>
        private long FatOffset(uint index)
        {
            if (index >= this.maxNumDataBlocks)
            {
                throw new InvalidFATIndexException();
            }

            return metaDataBlocks * blockSize + index * 4;
        }

        /// <summary>
        /// Write new value at index of the FAT.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        internal void FatSet(uint index, uint value)
        {
            fat[index] = value;
            Write((ulong)FatOffset(index), value);

            // Update firstFreeBlock if an earlier block was just freed above.
            // We're not using GetFreeBlock here since it has the side effect
            // that it checks whether firstFreeBlock actually is free, and
            // finds the next free block if it is not. Since we're possibly
            // updating firstFreeBlock, this would be wasteful.
            if (index < firstFreeBlock && value == freeBlock)
            {
                SetFirstFreeBlock(index);
            }
        }

        /// <summary>
        /// Set an entry in the FAT to end-of-cluster.
        /// </summary>
        /// <param name="index"></param>
        internal void FatSetEOC(uint index)
        {
            FatSet(index, endOfChain);
        }

        /// <summary>
        /// Make an entry in the FAT free.
        /// </summary>
        /// <param name="index"></param>
        internal void FatSetFree(uint index)
        {
            if(fat[index] != freeBlock) {
                SetFreeBlocks(freeBlocks + 1);
            }
            FatSet(index, freeBlock);
        }

        /// <summary>
        /// Get the FAT-index of the first free block and update the internal firstFreeBlock variable.
        /// </summary>
        /// <returns></returns>
        internal uint GetFreeBlock()
        {
            if (!(freeBlocks >= 1))
            {
                throw new NotEnoughSpaceException();
            }

            SetFreeBlocks(freeBlocks - 1);

            if (freeBlocks == 0)
            {
                SetFirstFreeBlock(endOfChain);
                return firstFreeBlock;
            }

            var returnBlock = firstFreeBlock;
            for (uint i = firstFreeBlock + 1; i < maxNumDataBlocks; i += 1)
            {
                if (fat[i] == freeBlock)
                {
                    //Console.WriteLine("Found a free block: fat[{0}] = {1}", i, fat[i]);
                    SetFirstFreeBlock(i);
                    break;
                }
            }
            return returnBlock;
        }

        /// <summary>
        /// Get the byte-offset in to the JCDVFS-file in to a block.
        /// </summary>
        /// <param name="dataBlock"></param>
        /// <param name="blockOffset"></param>
        /// <returns></returns>
        internal ulong BlockGetByteOffset(uint dataBlock, uint blockOffset)
        {
            if (dataBlock >= this.maxNumDataBlocks)
            {
                throw new InvalidFATIndexException();
            }
            return (dataOffsetBlocks + (ulong)dataBlock) * blockSize + blockOffset;
        }

        /// <summary>
        /// Get the byte-offset in to the blockIndex'th block of a file, using the FAT.
        /// </summary>
        /// <param name="firstBlock">First block of the file.</param>
        /// <param name="blockIndex">Block which you wish to index.</param>
        /// <param name="blockOffset">Offset in to the block wished to index.</param>
        /// <returns></returns>
        internal ulong FileGetByteOffset(uint firstBlock, uint blockIndex, uint blockOffset)
        {
            for (uint i = 0; i < blockIndex; i++)
            {
                if (fat[firstBlock] == endOfChain || fat[firstBlock] == freeBlock)
                {
                    throw new ReachedEndOfFileException();
                }

                firstBlock = fat[firstBlock];
            }
            return BlockGetByteOffset(firstBlock, blockOffset);
        }

        private void NewFSSetSize(ulong size)
        {
            // Adjust size so that it is a multiple of block size.
            size = Helpers.ruid(size, blockSize); // Round up to whole blocks.
            size -= metaDataBlocks; // Without metadata blocks.
            ulong sizeMultiple = 1 + fatEntriesPerBlock; // One FAT block + number of data blocks it represents.
            fatBlocks = (uint)Helpers.ruid(size, sizeMultiple); // Number of FAT blocks.
            fat = new uint[fatBlocks * fatEntriesPerBlock];

            InitSize(true);

            // These are written in newFSWriteMetaData()

            // The current number of unused blocks in the FS.
            // The JCDVFS-file might not yet be big enough to actually hold this many blocks!
            freeBlocks = fatBlocks * fatEntriesPerBlock - firstFreeBlock;
            firstFreeBlock = 3; // The first free block is after the (initially empty) root dir and search file blocks.

            hfsFileStream.SetLength((long)currentSize);
        }

        /// <summary>
        /// Initialize FAT (mark all unused blocks free) and write it to the JCDVFS-file.
        /// </summary>
        private void NewFSInitAndWriteFAT()
        {
            // Not using fatSet in this function because of performance issues.
            // (Really. It took me 60 seconds to write 2 MB.)
            Seek((ulong)FatOffset(0));
            for (uint i = 0; i < this.fatBlocks * fatEntriesPerBlock; i += 1)
            {
                // The array will be initialized with 0's, so initially, free
                // entries will (incorrectly) have 0 as value.
                if (fat[i] == 0)
                {
                    fat[i] = freeBlock;
                }
                hfsBinaryWriter.Write(fat[i]);
            }
        }

        private void InitSize(bool newFile)
        {
            dataOffsetBlocks = metaDataBlocks + fatBlocks;
            maxNumDataBlocks = Math.Min(fatBlocks * fatEntriesPerBlock, availableBlockNumbers);
            maxNumBlocks = dataOffsetBlocks + maxNumDataBlocks;
            if (newFile)
            {
                currentNumDataBlocks = 2; // We start with an empty root folder and a empty search file.
                currentNumBlocks = dataOffsetBlocks + currentNumDataBlocks;
            }
            else
            {
                currentNumBlocks = (ulong)(hfsFileStream.Length) / blockSize;
                currentNumDataBlocks = (uint)(currentNumBlocks - fatBlocks - metaDataBlocks);
            }
            // Cast here to get ulong multiplication, to avoid overflow.
            currentSize = currentNumBlocks * (ulong)blockSize;
            maxSize = maxNumBlocks * (ulong)blockSize;
        }

        private void ReadFAT()
        {
            // The is FAT placed contiously, starting from the first data block.
            Seek(metaDataBlocks * blockSize);
            ByteToUintConverter cnv = new ByteToUintConverter
            {
                bytes = hfsBinaryReader.ReadBytes((int)(fatBlocks * blockSize))
            };
            // We're assuming the FAT size in bytes fits into an int, since that's what ReadBytes accepts.
            // This means the FS can't be more than 2TB. This should probably be changed.
            fat = cnv.uints; // Don't trust fat.Length after this.
            //http://stackoverflow.com/questions/619041/what-is-the-fastest-way-to-convert-a-float-to-a-byte/619307#619307
        }

        /// <summary>
        /// Write meta data to JCDVFS-file. Must be called after newFSSetSize, since this function calculates some of the meta data.
        /// </summary>
        private void NewFSWriteMetaData()
        {
            // Go to start of JCDVFS-file and write meta data continuously.
            Seek(0L);
            hfsBinaryWriter.Write(magicNumber);
            hfsBinaryWriter.Write(blockSize); // Currently set to 4KB fixed size.
            hfsBinaryWriter.Write(fatBlocks); // Number of blocks that the FAT spans.
            hfsBinaryWriter.Write(freeBlocks); // Number of free blocks.
            hfsBinaryWriter.Write(firstFreeBlock); // First free block. Currently statically set to 2.
            hfsBinaryWriter.Write(rootDirBlock); // Currently statically set to 0.
            hfsBinaryWriter.Write(searchFileTreeBlock); // Currently statically set to 1.
            hfsBinaryWriter.Write(searchFileDataBlock); // Currently statically set to 2.
        }

        private void ParseMetaData()
        {
            Seek(0L);

            // Verify that we're reading a JCDVFS-file.
            uint tmp = hfsBinaryReader.ReadUInt32();
            if (tmp != magicNumber)
            {
                throw new InvalidFileException();
            }

            // Make sure that the block size is 2^12, since this isn't configurable yet.
            tmp = hfsBinaryReader.ReadUInt32();
            if (tmp != blockSize)
            {
                // Only JCDVFS-files with 4KB block sizes are supported.
                throw new InvalidFileException();
            }

            fatBlocks = hfsBinaryReader.ReadUInt32();
            freeBlocks = hfsBinaryReader.ReadUInt32();
            firstFreeBlock = hfsBinaryReader.ReadUInt32();
            //rootDirBlock = br.ReadUInt32(); // Statically set
            //searchFileTreeBlock = br.ReadUInt32(); // Statically set.
            //searchFileDataBlock = br.ReadUInt32(); // Statically set.
        }

        private void NewFSCreateRootFolder()
        {
            // The FAT is updated in JCDFolder.createRootFolder, which is why that's not done here.
            this.rootFolder = JCDFolder.createRootFolder(this);
            this.currentFolder = rootFolder;
        }

        private void InitRootFolder()
        {
            this.rootFolder = JCDFolder.rootFolder(this);
            this.currentFolder = rootFolder;
        }

        private void NewFSCreateSearchFile()
        {
            CreateHiddenFileDelegate f = (fileName, firstBlock) => {
                
                FatSetEOC(firstBlock);

                var entry = new JCDDirEntry {
                    Name = Helpers.PathGetFileName(fileName),
                    Size = 0,
                    IsFolder = false,
                    FirstBlock = firstBlock,
                };
                var container = GetFile(Helpers.PathGetDirectoryName(fileName));
                return ((JCDFolder)container).AddDirEntry(entry);
            };

            var treeFileStream = new JCDFileStream(f(searchFileTreePath, searchFileTreeBlock), OnFileModified);
            var dataFileStream = new JCDFileStream(f(searchFileDataPath, searchFileDataBlock), OnFileModified);

            fileIndex = FileIndex.Initialize(treeFileStream, dataFileStream);
            fileIndex.Close();
            InitSearchFile();
        }


        private void InitSearchFile() {
            var treeFile = GetFile(searchFileTreePath);
            var dataFile = GetFile(searchFileDataPath);

            var treeFileStream = new JCDFileStream(treeFile, OnFileModified);
            var dataFileStream = new JCDFileStream(dataFile, OnFileModified);

            fileIndex = FileIndex.Open(treeFileStream, dataFileStream);
            
            // Add event handlers
            FileAdded += path => {
                fileIndex.Put(path);
            };

            FileDeleted += path => {
                fileIndex.Remove(path);
            };

            FileMoved += (oldPath, newPath) => {
                fileIndex.Rename(oldPath, newPath);
            };
        }

        /// <summary>
        /// Walk FAT chain starting from firstEntry, and inform `v` of all blocks.
        /// </summary>
        /// <typeparam name="T">Instance of IVisitor</typeparam>
        /// <param name="firstBlock">Index of first FAT entry</param>
        /// <param name="v">Instance of IVisitor</param>
        /// <returns></returns>
        internal IVisitor WalkFATChain(uint firstEntry, IVisitor v)
        {
            // firstBlock didn't point to a valid starting point of a file.
            // Assumes that the reserved block numbers are placed continuously from 0.
            if (fat[firstEntry] == freeBlock || fat[firstEntry] < reservedBlockNumbers)
            {
                return v;
            }

            // Check whether the visitor wants to visit the next block.
            bool continue_ = true;
            var entry = endOfChain;
            var nextEntry = firstEntry;
            
            while (continue_ && nextEntry != endOfChain && nextEntry != freeBlock)
            {
                entry = nextEntry;
                nextEntry = fat[entry];
                continue_ = ((IVisitor)(v)).Visit(this, entry);
            }
            return v;
        }

        /// <summary>
        /// Allocate the minimum amount of blocks in which `size` bytes can be contained.
        /// </summary>
        /// <returns>Index of first block</returns>
        internal uint AllocateBlocks(ulong size)
        {
            // Make sure that there are enough free blocks.
            long blocksRequired = Math.Max((uint)Helpers.ruid(size, JCDFAT.blockSize), 1);

            if (!(freeBlocks >= blocksRequired))
            {
                throw new NotEnoughSpaceException();
            }

            uint firstBlock = GetFreeBlock();
            var nextBlock = firstBlock;
            uint prevBlock;
            // Chain blocks in FAT. Make sure that we mark last block as EOC.
            for (int i = 2; i <= blocksRequired; i++) {
                prevBlock = nextBlock;
                nextBlock = GetFreeBlock();
                FatSet(prevBlock, nextBlock);
                if (i % 10000 == 0) {
                    Console.WriteLine("Allocated {0} blocks.", i);
                }
            }
            FatSetEOC(nextBlock);
            if(blocksRequired >= 10000) {
                Console.WriteLine("Done allocating space.");
            }
            return firstBlock;
        }

        public void Dispose()
        {
            Close();
        }

        public void Close()
        {
            fileIndex.Close();
            hfsBinaryWriter.Flush();
            hfsFileStream.Flush();
            hfsBinaryWriter.Dispose();
            hfsBinaryReader.Dispose();
            hfsFileStream.Dispose();
        }

        public ulong Size()
        {
            return this.maxSize;
        }

        public ulong FreeSpace()
        {
            // Cast here to get ulong multiplication, to avoid overflow.
            return this.freeBlocks * (ulong)JCDFAT.blockSize;
        }

        public ulong OccupiedSpace() {
            return Size() - FreeSpace();
        }

        private static JCDFile BrowseStep(JCDFolder folder, string step) {
            step = Helpers.TrimLastSlash(step);
            if(step == "..") {
                return folder.Parent;
            }
            else if(step == ".") {
                return folder;
            }
            else {
                return folder.GetFile(step);
            }
        }

        private JCDFile GetFile(string path) {
            JCDFolder ret;
            if(path.StartsWith("/")) { //Offset folder
                ret = rootFolder;
            }
            else {
                ret = currentFolder;
            }

            string[] segments = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if(segments.Length == 0) {
                return ret;
            }

            int i;
            for(i = 0; i < segments.Length - 1; i++ ) {
                var tmp = BrowseStep(ret, segments[i]);
                if(tmp == null || !tmp.IsFolder) {
                    return null;
                }
                ret = (JCDFolder) tmp;
            }

            return BrowseStep(ret, segments[i]);
        }

        public void SetCurrentDirectory(string vfsPath) {
            if (vfsPath == "~") {
                currentFolder = rootFolder;
                return;
            }

            var newDir = GetFile(vfsPath);
            if(newDir == null) {
                throw new vfs.exceptions.FileNotFoundException();
            }
            if(!newDir.IsFolder) {
                throw new NotAFolderException();
            }
            currentFolder = (JCDFolder) newDir;
        }

        public void CreateDirectory(string vfsPath, bool createParents) {
            if (createParents) {
                CreateParents(vfsPath);
            }
            CreateFile(JCDFAT.blockSize, vfsPath, true);
        }

        private void CreateParents(string path) {
            var parents = Helpers.PathGetDirectoryName(path).Split('/');

            // Can't create root directory.
            if (parents.Length == 1 && parents[0] == ".") {
                return;
            }

            string currentPath = ".";
            if (path.StartsWith("/")) {
                currentPath = "/";
            }
            JCDFile f = GetFile(currentPath);
            // Create parents that don't exist.
            foreach (var dir in parents) {
                if (String.IsNullOrEmpty(dir)) {
                    continue;
                }
                currentPath += dir + "/";
                // Check whether directory already exists, but only if the previous
                // directory existed.
                if (f != null) { // Only use GetFile if all previous directories existed.
                    f = GetFile(currentPath);
                }
                // Create directory if it doesn't exist.
                if (f == null) {
                    CreateDirectory(currentPath, false);
                }
            }
        }
        

        private JCDFile CreateFile(ulong size, string path, bool isFolder)
        {
            var entry = new JCDDirEntry
            {
                Name = Helpers.PathGetFileName(path),
                Size = size,
                IsFolder = isFolder,
                FirstBlock = AllocateBlocks(size),
            };

            // Clear folder in case it holds old data, so that all entries become "final".
            if (isFolder)
            {
                ZeroBlock(entry.FirstBlock);
            }

            var container = GetFile(Helpers.PathGetDirectoryName(path));

            if(container == null) {
                throw new ParentNotFoundException();
            }
            if(!container.IsFolder) {
                throw new NotAFolderException();
            }
            
            var result = ((JCDFolder) container).AddDirEntry(entry);
            OnFileAdded(result.Path);
            return result;
        }

        internal void ZeroBlock(uint block)
        {
            var zeros = new byte[JCDFAT.blockSize];
            Array.Clear(zeros, 0, zeros.Length);
            Write(BlockGetByteOffset(block, 0), zeros);
        }

        private void ImportFolder(string hfsFolderPath, string vfsPath) {
            var top = (JCDFolder) CreateFile(JCDFAT.blockSize, vfsPath, true);

            ImportFolderRecursive(top, hfsFolderPath);
        }

        private void ImportFolderRecursive(JCDFolder parentFolder, string hfsFolderPath)
        {
            // Import files from hfsFolderPath
            var files = Directory.GetFiles(hfsFolderPath); // Returns list of full file paths on hfs.
            foreach (var filePath in files)
            {
                FileStream fs = null;
                try
                {
                    fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    ImportFile(fs, parentFolder.FileGetPath(Helpers.PathGetFileName(filePath.Replace('\\', '/')), false), fs.Name);
                }
                finally
                {
                    fs.Close();
                }
            }

            // Import folders from hfsFolderPath
            var folders = Directory.GetDirectories(hfsFolderPath); // Returns list of full folder paths on hfs.
            foreach (var folderPath in folders)
            {
                // Create folder in vfs.
                var fPath = folderPath.Replace('\\', '/');
                var folderName = Helpers.PathGetFileName(Helpers.TrimLastSlash(fPath));
                var folder = (JCDFolder) CreateFile(JCDFAT.blockSize, parentFolder.FileGetPath(folderName, true), true);
                ImportFolderRecursive(folder, fPath);
            }
        }

        public void CreateFile(string vfsPath, ulong size, bool createParents) {
            if (createParents) {
                CreateParents(vfsPath);
            }
            CreateFile(size, vfsPath, false);
        }

        public void ImportFile(string hfsPath, string vfsPath) {
            FileStream fileToImport = null;
            if(Directory.Exists(hfsPath)) {
                ImportFolder(hfsPath, vfsPath);
            }
            else {
                try {
                    fileToImport = new FileStream(hfsPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    ImportFile(fileToImport, vfsPath, fileToImport.Name);
                }
                finally {
                    fileToImport.Close();
                }
            }
        }

        private void ImportFile(Stream file, string path, string fileName) {
            uint firstBlock = CreateFile((ulong) file.Length, path, false).Entry.FirstBlock;
            uint bufPos = readBufferSize * blockSize;
            int bufSize = (int)bufPos;
            byte[] buffer = new byte[bufSize];

            WalkFATChain(firstBlock, new FileImporterVisitor((ulong)file.Length, buffer, () => {
                bufPos += blockSize;
                if (bufPos >= bufSize)
                {
                    file.Read(buffer, 0, bufSize);
                    bufPos = 0;
                    return bufPos;
                }
                return bufPos;
            }));
            if(fileName != null) {
                Console.WriteLine("Imported {0} to {1}", fileName, path);
            }
        }

        /// <summary>
        /// Write `data` in file, starting from `offset` (in bytes).
        /// 
        /// Assumes that there are enough blocks available to write the contents of `data` to file.
        /// </summary>
        /// <param name="data">Data to be written</param>
        /// <param name="offset">Offset in to file, in bytes</param>
        /// <param name="file">The first block of the file to be written.</param>
        public void WriteFile(byte[] data, long offset, uint firstBlock) {
            WalkFATChain(firstBlock, new FileWriterVisitor(data, offset));
        }

        public void ReadFile(byte[] buffer, ulong offset, ulong count, uint firstBlock) {
            int bufferPos = 0;
            WalkFATChain(firstBlock, new FileReaderVisitor(count, offset, (data, lastBlock) => {
                Buffer.BlockCopy(data, 0, buffer, bufferPos, data.Length);
                bufferPos += data.Length;
                return true;
            }));
        }

        private void ExportFolderRecursive(JCDFolder folder, string hfsPath)
        {
            foreach (var file in folder.GetFileEntries()) {
                if(file.EntryIsEmpty() || file.EntryIsFinal()) {
                    continue;
                }
                // Export folder.
                if (file.IsFolder) {
                    string folderPath = Helpers.PathCombine(hfsPath, file.Name);
                    Directory.CreateDirectory(folderPath);
                    ExportFolderRecursive((JCDFolder)file, folderPath);
                }
                else { // Export file.
                    ExportFile(hfsPath, file);
                }
            }
        }

        private void ExportFile(string hfsPath, JCDFile file) {
            FileStream outputFile = null;

            // Make sure parent directory exists on hfs.
            var dirName = Path.GetDirectoryName(hfsPath);
            // In case hfsPath is C:\\, dirName would return null. But
            // in the same case, GetPathRoot wouldn't.
            // We use this to make sure that dirName is a valid directory.
            if (dirName == null && Path.GetPathRoot(hfsPath) != null) {
                dirName = hfsPath;
            }
            if (!Directory.Exists(dirName)) {
                throw new vfs.exceptions.FileNotFoundException();
            }

            // If given path points to a directory, append file name to it.
            string filePath;
            if (Directory.Exists(hfsPath)) {
                filePath = Helpers.PathCombine(hfsPath, file.Name);
            }
            else { // Else let the given path decide the name of the file exported.
                filePath = hfsPath;
            }
            
            try {
                outputFile = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                ExportFile(outputFile, file);
            }
            finally {
                outputFile.Close();
            }
        }

        public void ExportFile(string vfsPath, string hfsPath)
        {

            var file = GetFile(vfsPath);
            if (file == null)
            {
                throw new vfs.exceptions.FileNotFoundException();
            }

            // Export folder
            if (file.IsFolder) {
                Directory.CreateDirectory(hfsPath);
                ExportFolderRecursive((JCDFolder)file, hfsPath);
                return;
            }

            // Export file
            ExportFile(hfsPath, file);
        }

        private void ExportFile(Stream outputFile, JCDFile file) {
            int bufSize = (int)(readBufferSize * blockSize);
            var buffer = new byte[bufSize];
            int bufPos = 0;

            WalkFATChain(file.Entry.FirstBlock, new FileReaderVisitor(file.Size, (blockData, lastBlock) => {
                // If buffer overruns when reading this block, write buffer to file.
                if (bufPos >= bufSize)
                {
                    outputFile.Write(buffer, 0, bufSize);
                    bufPos = 0;
                }

                Buffer.BlockCopy(blockData, 0, buffer, bufPos, blockData.Length);
                bufPos += blockData.Length;

                if (lastBlock)
                {
                    outputFile.Write(buffer, 0, bufPos);
                }

                return true;
            }));
        }

        public JCDDirEntry GetFileDetails(string vfsPath)
        {
            var file = GetFile(vfsPath);
            if (file == null)
                throw new vfs.exceptions.FileNotFoundException();

            return file.Entry;
        }

        public JCDDirEntry[] ListDirectory(string vfsPath)
        {
            var directory = GetFile(vfsPath);
            if(directory == null) {
                throw new vfs.exceptions.FileNotFoundException();
            }
            if(!directory.IsFolder) {
                throw new NotAFolderException();
            }
            var files = ((JCDFolder)directory).GetFileEntries();
            var notNulls = files.Where(file => { return !(file.EntryIsEmpty() ||
                                                          file.EntryIsFinal() ||
                                                          file.Entry.FirstBlock == searchFileDataBlock||
                                                          file.Entry.FirstBlock == searchFileTreeBlock);
            });
            return notNulls.Select(file => { return file.Entry; }).ToArray();
        }

        public void DeleteFile(string vfsPath, bool recursive)
        {
            var file = GetFile(vfsPath);
            if (file == null)
            {
                throw new vfs.exceptions.FileNotFoundException();
            }
            if (file.IsFolder && !recursive)
            {
                throw new NonRecursiveDeletionException();
            }
            file.Delete(false);
        }

        public void RenameFile(string vfsPath, string newName) {
            var file = GetFile(vfsPath);
            if(file == null) {
                throw new vfs.exceptions.FileNotFoundException();
            }
            if(file.Parent.GetFile(newName) != null) {
                throw new FileExistsException();
            }
            if(!Helpers.FileNameIsValid(newName)) {
                throw new InvalidFileNameException();
            }
            file.Name = newName;
        }

        public void MoveFile(string vfsPath, string newVfsPath) {
            var fromFile = GetFile(vfsPath);

            // Insert file in to destination.
            var toFolderTmp = GetFile(Helpers.PathGetDirectoryName(newVfsPath));
            if(toFolderTmp == null) {
                throw new vfs.exceptions.FileNotFoundException();
            }
            if(!toFolderTmp.IsFolder) {
                throw new NotAFolderException();
            }
            var toFolder = ((JCDFolder) toFolderTmp);
            var newName = Helpers.PathGetFileName(newVfsPath);
            if(toFolder.GetFile(newName) != null) {
                throw new FileExistsException();
            }
            if(fromFile.IsFolder) {
                if(((JCDFolder) fromFile).IsParentOf(toFolder)) { //Also checks equality
                    throw new MoveToSelfException();
                }
            }
            var toEntry = fromFile.Entry;
            toEntry.Name = Helpers.PathGetFileName(newVfsPath);
            var toFile = toFolder.AddDirEntry(toEntry);

            // Trigger FileMoved event.
            OnFileMoved(fromFile.Path, toFile.Path);
            // Update the path of sub-files, if moved file is a folder.
            if (toFile.IsFolder) {
                ((JCDFolder)toFile).UpdateChildrenPaths();
            }

            // Delete original file.
            fromFile.DeleteEntry();
        }

        private void CopyFile(JCDFile oldFile, string newVfsPath) {
            if(oldFile.IsFolder) {
                var newFolder = (JCDFolder)CreateFile(oldFile.Size, newVfsPath, true);
                var files = ((JCDFolder) oldFile).GetFileEntries();
                foreach(var file in files) {
                    if(file.EntryIsEmpty() || file.EntryIsFinal()) {
                        continue;
                    }
                    string path = newFolder.FileGetPath(file.Name, false); //Doesn't matter if it's a folder
                    CopyFile(file, path);
                }
            }
            else {
                MemoryStream ms = new MemoryStream((int)Math.Min(oldFile.Size, (ulong) Int32.MaxValue));
                ExportFile(ms, oldFile);
                ImportFile(ms, newVfsPath, null);
            }
        }

        public void CopyFile(string vfsPath, string newVfsPath) {
            var fromFile = GetFile(vfsPath);
            if(fromFile == null) {
                throw new vfs.exceptions.FileNotFoundException();
            }

            CopyFile(fromFile, newVfsPath);
        }

        internal void tryShrink() {
            long lastUsedBlock = (hfsFileStream.Length - dataOffsetBlocks * blockSize) / blockSize - 1;
            long i;
            for(i = lastUsedBlock; fat[i] == freeBlock; i--);

            if(i < lastUsedBlock) {
                hfsFileStream.SetLength((i + 1 + dataOffsetBlocks) * blockSize);
            }
        }

        public string GetCurrentDirectory() {
            return currentFolder.Path;
        }

        /// <summary>
        /// Search through a specific folder in the file system.
        /// </summary>
        /// <param name="searchPath">Folder to search.</param>
        /// <param name="fileName">Exact name of the file you want to find.</param>
        /// <param name="caseSensitive">Whether the search be case sensitive</param>
        /// <param name="recursive">Whether the search be recursive</param>
        /// <returns></returns>
        public string[] Search(string searchPath, string fileName, bool caseSensitive, bool recursive) {
            var files = fileIndex.Get(fileName, caseSensitive);
            if (files == null) {
                return new string[0];
            }

            IEnumerable<IndexedFile> filtered = files;
            if (recursive) {
                filtered = files.Where(f => f.Path.StartsWith(searchPath));
            } else {
                filtered = files.Where(f => f.Path == Path.Combine(searchPath, f.Name));
            }
            return filtered.Select(f => f.Path).ToArray();
        }

        /// <summary>
        /// Search recursively in a specific folder of the file system.
        /// </summary>
        /// <param name="searchPath"></param>
        /// <param name="fileName"></param>
        /// <param name="caseSensitive"></param>
        /// <returns></returns>
        public string[] Search(string searchPath, string fileName, bool caseSensitive) {
            return Search(searchPath, fileName, caseSensitive, true);
        }

        /// <summary>
        /// Search the entire file system for a specific file name.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="caseSensitive"></param>
        /// <returns></returns>
        public string[] Search(string fileName, bool caseSensitive) {
            return Search(rootFolder.Path, fileName, caseSensitive);
        }

        public JCDFileStream GetFileStream(string vfsPath) {
            var file = GetFile(vfsPath);
            return new JCDFileStream(file, OnFileModified);
        }
    }
}
