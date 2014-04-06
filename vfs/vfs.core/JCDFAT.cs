using System;
using System.Linq;
using System.IO;
using vfs.exceptions;
using vfs.core.visitor;
using System.Collections.Generic;

namespace vfs.core
{
    public class JCDFAT : IDisposable
    {
        private bool initialized = false;

        private const uint magicNumber = 0x13371337;
        private const uint freeBlock = 0xFFFFFFFF;
        private const uint endOfChain = 0xFFFFFFFE;

        public const uint rootDirBlock = 0;
        private const uint searchFileBlock = 1;

        private const uint readBufferSize = 50 * 1024; //In blocks

        // All sizes in this class are given in bytes unless otherwise specified.
        private const uint reservedBlockNumbers = 2; // End-of-chain and free
        // See this stackoverflow answer for bit shifting behaviour in c#: http://stackoverflow.com/questions/9210373/why-do-shift-operations-always-result-in-a-signed-int-when-operand-is-32-bits
        private const uint availableBlockNumbers = (uint)((1L << 32) - reservedBlockNumbers); // 32-bit block numbers
        private const uint metaDataBlocks = 1; // Number of blocks used for meta data (doesn't include the FAT)
        public const uint blockSize = 1 << 12; // 4KB blocks
        public const ulong globalMaxFSSize = (ulong)availableBlockNumbers * blockSize + (1L << 32) * 4 + metaDataBlocks * blockSize;
        //numBlocks * (blockSize + fatEntrySize) + metaDataSize. FAT size is rounded up to a whole number of blocks, assuming reservedBlockNumbers < blockSize/4.
        public const uint dirEntrySize = 1 << 8; // 256B == JCDDirEntry.StructSize();
        public const uint fatEntriesPerBlock = blockSize / 4;
        public const uint dirEntriesPerBlock = blockSize / dirEntrySize;

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

        private FileStream fs;
        private BinaryWriter bw;
        private BinaryReader br;


        /// <summary>
        /// Create a new JCDFAT-file.
        /// </summary>
        /// <param name="fs">Stream to a file open with read/write access.</param>
        /// <param name="size">Maximum size of the new JCDVFS-file.</param>
        public JCDFAT(FileStream fs, ulong size)
        {
            this.fs = fs;

            // TODO: Make sure that the file is empty??

            NewFSSetSize(size);

            bw = new BinaryWriter(fs);
            br = new BinaryReader(fs);

            NewFSWriteMetaData();
            NewFSInitAndWriteFAT();
            NewFSCreateRootFolder();
            NewFSCreateSearchFile();
            // Make sure that the file system is written to disk.
            bw.Flush();

            initialized = true;
        }

        /// <summary>
        /// Open an existing JCDFAT-file.
        /// </summary>
        /// <param name="fs">Stream to a JCDVFS-file, open with read/write access.</param>
        public JCDFAT(FileStream fs)
        {
            this.fs = fs;

            bw = new BinaryWriter(fs);
            br = new BinaryReader(fs);

            ParseMetaData();
            InitSize(false);
            ReadFAT();
            InitRootFolder();
            InitSearchFile();

            initialized = true;
            var rootDir = (BlockCounterVisitor)WalkFATChain(rootDirBlock, new BlockCounterVisitor());
            Console.WriteLine("Root dir spans {0} blocks", rootDir.Blocks);
            var searchFile = (BlockCounterVisitor)WalkFATChain(searchFileBlock, new BlockCounterVisitor());
            Console.WriteLine("Search file spans {0} blocks", searchFile.Blocks);
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
        public void Write(ulong offset, byte[] data, int arrOffset, int count)
        {
            Seek(offset);
            fs.Write(data, arrOffset, count);
        }

        /// <summary>
        /// Write data to JCDVFS-file.
        /// </summary>
        /// <param name="offset">Absolute offset in to JCDVFS-file at which to start writing data.</param>
        /// <param name="data">Data to write to JCDVFS-file.</param>
        public void Write(ulong offset, byte[] data)
        {
            Seek(offset);
            bw.Write(data);
        }

        /// <summary>
        /// Write data to JCDVFS-file.
        /// </summary>
        /// <param name="offset">Absolute offset in to JCDVFS-file at which to start writing data.</param>
        /// <param name="data">Data to write to JCDVFS-file.</param>
        public void Write(ulong offset, ushort data)
        {
            Seek(offset);
            bw.Write(data);
        }

        /// <summary>
        /// Write data to JCDVFS-file.
        /// </summary>
        /// <param name="offset">Absolute offset in to JCDVFS-file at which to start writing data.</param>
        /// <param name="data">Data to write to JCDVFS-file.</param>
        public void Write(ulong offset, uint data)
        {
            Seek(offset);
            bw.Write(data);
        }

        private void Seek(ulong offset)
        {
            if (fs.Position == (long)offset)
            {
                return;
            }
            fs.Seek((long)offset, SeekOrigin.Begin);
        }

        /// <summary>
        /// Read raw data from the JCDVFS-file.
        /// </summary>
        /// <param name="offset">Byte-offset of point to start reading from.</param>
        /// <param name="length">Length in bytes.</param>
        /// <returns>Byte array of length `length`.</returns>
        public byte[] Read(ulong offset, uint length)
        {
            Seek(offset);
            return br.ReadBytes((int)length);
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
                throw new Exception("The FAT doesn't have that many entries!");
            }

            return metaDataBlocks * blockSize + index * 4;
        }

        /// <summary>
        /// Write new value at index of the FAT.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public void FatSet(uint index, uint value)
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
        public void FatSetEOC(uint index)
        {
            FatSet(index, endOfChain);
        }

        /// <summary>
        /// Make an entry in the FAT free.
        /// </summary>
        /// <param name="index"></param>
        public void FatSetFree(uint index)
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
        public uint GetFreeBlock()
        {
            if (!(freeBlocks >= 1))
            {
                throw new Exception("No more free blocks!");
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
        public ulong BlockGetByteOffset(uint dataBlock, uint blockOffset)
        {
            if (dataBlock >= this.maxNumDataBlocks)
            {
                throw new Exception("There aren't that many data blocks!");
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
        public ulong FileGetByteOffset(uint firstBlock, uint blockIndex, uint blockOffset)
        {
            for (uint i = 0; i < blockIndex; i++)
            {
                if (fat[firstBlock] == endOfChain || fat[firstBlock] == freeBlock)
                {
                    throw new Exception("File doesn't have that many blocks!");
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
            firstFreeBlock = 2; // The first free block is after the (initially empty) root dir and search file block.

            fs.SetLength((long)currentSize);
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
                bw.Write(fat[i]);
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
                currentNumBlocks = (ulong)(fs.Length) / blockSize;
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
                bytes = br.ReadBytes((int)(fatBlocks * blockSize))
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
            bw.Write(magicNumber);
            bw.Write(blockSize); // Currently set to 4KB fixed size.
            bw.Write(fatBlocks); // Number of blocks that the FAT spans.
            bw.Write(freeBlocks); // Number of free blocks.
            bw.Write(firstFreeBlock); // First free block. Currently statically set to 2.
            bw.Write(rootDirBlock); // Currently statically set to 0.
            bw.Write(searchFileBlock); // Currently statically set to 1.
        }

        private void ParseMetaData()
        {
            Seek(0L);

            // Verify that we're reading a JCDVFS-file.
            uint tmp = br.ReadUInt32();
            if (tmp != magicNumber)
            {
                throw new InvalidFileException();
            }

            // Make sure that the block size is 2^12, since this isn't configurable yet.
            tmp = br.ReadUInt32();
            if (tmp != blockSize)
            {
                // Only JCDVFS-files with 4KB block sizes are supported.
                throw new InvalidFileException();
            }

            fatBlocks = br.ReadUInt32();
            freeBlocks = br.ReadUInt32();
            firstFreeBlock = br.ReadUInt32();
            //rootDirBlock = br.ReadUInt32(); // Unused.
            //searchFileBlock = br.ReadUInt32(); // Unused.
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
            //Not implemented
            FatSetEOC(searchFileBlock);
        }

        private void InitSearchFile()
        {
            //Not implemented
        }

        /// <summary>
        /// Walk FAT chain starting from firstEntry, and inform `v` of all blocks.
        /// </summary>
        /// <typeparam name="T">Instance of IVisitor</typeparam>
        /// <param name="firstBlock">Index of first FAT entry</param>
        /// <param name="v">Instance of IVisitor</param>
        /// <returns></returns>
        public IVisitor WalkFATChain(uint firstEntry, IVisitor v)
        {
            // firstBlock didn't point to a valid starting point of a file.
            // Assumes that the reserved block numbers are placed continuously from 0.
            if (fat[firstEntry] == freeBlock || fat[firstEntry] < reservedBlockNumbers)
            {
                return v;
            }

            // Figure out nextEntry before letting visitor visit. This is done later as well.
            // We do this because the visitor might change the value of the block we give him!
            uint entry = firstEntry;
            uint nextEntry = fat[entry];

            // Check whether the visitor wants to visit the next block.
            bool continue_ = v.Visit(this, entry);
            
            while (continue_ && nextEntry != endOfChain && nextEntry != freeBlock &&
                   nextEntry != rootDirBlock && nextEntry != searchFileBlock)
            {
                entry = nextEntry;
                nextEntry = fat[entry];
                continue_ = ((IVisitor)(v)).Visit(this, entry);
            }
            if (entry != firstEntry) {
                ((IVisitor)(v)).Visit(this, entry);
            }
            return v;
        }

        /// <summary>
        /// Allocate the minimum amount of blocks in which `size` bytes can be contained.
        /// </summary>
        /// <returns>Index of first block</returns>
        public uint AllocateBlocks(ulong size)
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
            for (int i = 0; i < blocksRequired - 1; i += 1)
            {
                prevBlock = nextBlock;
                nextBlock = GetFreeBlock();
                FatSet(prevBlock, nextBlock);
                if (i % 10000 == 0)
                {
                    Console.WriteLine("Allocated {0} blocks.", i);
                }
            }
            FatSetEOC(nextBlock);
            return firstBlock;
        }

        public void Dispose()
        {
            Close();
        }

        public void Close()
        {
            bw.Flush();
            fs.Flush();
            bw.Dispose();
            br.Dispose();
            fs.Dispose();
        }

        public ulong GetSize()
        {
            return this.maxSize;
        }

        public ulong GetFreeSpace()
        {
            // Cast here to get ulong multiplication, to avoid overflow.
            return this.freeBlocks * (ulong)JCDFAT.blockSize;
        }

        private JCDFile BrowseStep(JCDFolder folder, string step) {
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

            /*if(!path.IsAbsoluteUri) {
                path = new Uri(ret.Path, path);
            }
            if(path.Equals(ret.Path)) {
                return ret;
            }
            string[] segments = ret.Path.MakeRelativeUri(path).ToString().Split(new char[] {'/'});*/
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

        public void SetCurrentDirectory(string path) {
            var newDir = GetFile(path);
            if(newDir == null || !newDir.IsFolder) {
                //TODO: proper exception
                throw new Exception("No such folder!");
            }
            currentFolder = (JCDFolder) newDir;
        }

        public void CreateFolder(string path) {
            CreateFile(JCDFAT.blockSize, path, true);
        }

        private JCDFile CreateFile(ulong size, string path, bool isFolder)
        {
            // TODO: Make sure that fileName is not longer than allowed by dirEntry.
            // This should probably be checked in JCDDirEntry constructor.

            // TODO: We need to use `path` for something.

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
            if(container == null || !container.IsFolder) {
                throw new Exception("No such folder!");
            }

            return ((JCDFolder) container).AddDirEntry(entry);
        }

        public void ZeroBlock(uint block)
        {
            var zeros = new byte[JCDFAT.blockSize];
            Array.Clear(zeros, 0, zeros.Length);
            Write(BlockGetByteOffset(block, 0), zeros);
        }

        public void ImportFolder(string hfsFolderPath, string vfsPath) {
            /*var parentDirTmp = GetFile(Helpers.PathGetDirectoryName(vfsPath));
            if(parentDirTmp == null || !parentDirTmp.IsFolder) {
                //TODO: proper exception
                throw new Exception("No such folder!");
            }
            var parentDir = (JCDFolder) parentDirTmp;*/
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
                    ImportFile(fs, parentFolder.FileGetPath(Helpers.PathGetFileName(filePath.Replace('\\', '/')), false));
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

        public void ImportFile(FileStream file, string path) {
            uint firstBlock = CreateFile((ulong) file.Length, path, false).Entry.FirstBlock;
            uint bufPos = readBufferSize * blockSize;
            int bufSize = (int)bufPos;
            byte[] buffer = new byte[bufSize];

            WalkFATChain(firstBlock, new FileWriterVisitor((ulong)file.Length, buffer, () => {
                bufPos += blockSize;
                if (bufPos >= bufSize)
                {
                    file.Read(buffer, 0, bufSize);
                    bufPos = 0;
                    return bufPos;
                }
                return bufPos;
            }));
            Console.WriteLine("Imported {0} to {1}", path, file.Name);
        }

        private void ExportFolderRecursive(JCDFolder folder, string hfsPath)
        {
            foreach (var file in folder.GetFileEntries()) {
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
            var filePath = Helpers.PathCombine(hfsPath, file.Name);
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

        private void ExportFile(FileStream outputFile, JCDFile file) {
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

        public JCDDirEntry[] ListDirectory(string vfsPath)
        {
            var directory = GetFile(vfsPath);
            if(directory == null || !directory.IsFolder) {
                throw new Exception("No such folder!");
            }
            var files = ((JCDFolder)directory).GetFileEntries();
            var notNulls = files.Where(file => { return !(file.EntryIsEmpty() || file.EntryIsFinal()); });
            return notNulls.Select(file => { return file.Entry; }).ToArray();
        }

        public void DeleteFile(string path, bool recursive)
        {
            // TODO: Check if path is relative/absolute and retrieve parent folder of file.

            //path = Helpers.PathGetDirectoryName(path);
            var file = GetFile(path);
            if (file == null)
            {
                throw new vfs.exceptions.FileNotFoundException();
            }
            if (file.IsFolder && !recursive)
            {
                // TODO: Throw proper exception.
                throw new Exception("Can't delete a folder when the recursive flag is not set!");
            }
            file.Delete(false);
        }

        public void RenameFile(string vfsPath, string newName) {
            // TODO: Make sure that newName is a valid name.
            // TODO: Implement using fat.GetFile.
            var file = GetFile(vfsPath);
            if(file == null) {
                throw new vfs.exceptions.FileNotFoundException();
            }
            if(file.Parent.GetFile(newName) != null) {
                //TODO: real exception
                throw new Exception("There's already a file with that name!");
            }
            file.Name = newName;
        }
        public void MoveFile(string vfsPath, string newVfsPath) {
            // TODO: Implement using fat.GetFile.

            // Get original file
            //var fromFolder = (JCDFolder) null; // fat.GetFile(vfsPath);
            //var fromFileName = Helpers.PathGetFileName(vfsPath);
            //var fromFile = fromFolder.GetFile(fromFileName);
            var fromFile = GetFile(vfsPath);

            // Insert file in to destination.
            var toFolderTmp = GetFile(Helpers.PathGetDirectoryName(newVfsPath));
            if(toFolderTmp == null) {
                throw new vfs.exceptions.FileNotFoundException();
            }
            if(!toFolderTmp.IsFolder) {
                //TODO: real exception
                throw new Exception("Not a folder!");
            }
            var toFolder = ((JCDFolder) toFolderTmp);
            var newName = Helpers.PathGetFileName(newVfsPath);
            if(toFolder.GetFile(newName) != null) {
                //TODO: real exception
                throw new Exception("There's already a file with that name!");
            }
            if(fromFile.IsFolder) {
                if(((JCDFolder) fromFile).IsParentOf(toFolder)) { //Also checks equality
                    //TODO: real exception
                    throw new Exception("Cannot copy folder into itself!");
                }
            }
            var toEntry = fromFile.Entry;
            toEntry.Name = Helpers.PathGetFileName(newVfsPath);
            var toFile = toFolder.AddDirEntry(toEntry);

            // Delete original file.
            fromFile.DeleteEntry();
        }

        public void tryShrink() {
            long lastUsedBlock = (fs.Length - dataOffsetBlocks * blockSize) / blockSize - 1;
            long i;
            for(i = lastUsedBlock; fat[i] == freeBlock; i--);

            if(i < lastUsedBlock) {
                fs.SetLength((i + 1 + dataOffsetBlocks) * blockSize);
            }
        }

        public string GetCurrentDirectory() {
            return this.currentFolder.Path;
        }
    }
}
