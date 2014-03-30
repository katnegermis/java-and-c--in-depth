﻿using System;
using vfs.exceptions;
using System.IO;

namespace vfs.core
{
    class JCDFAT : IDisposable
    {
        private bool initialized = false;
        private long currentFileOffset = 0;

        private const uint magicNumber = 0x13371337;
        private const uint freeBlock = 0xFFFFFFFF;
        private const uint endOfChain = 0xFFFFFFFE;

        public const uint rootDirBlock = 0;
        private const uint searchFileBlock = 1;

        // All sizes in this class are given in bytes unless otherwise specified.
        private const uint reservedBlockNumbers = 2; //End-of-chain and free
        // See this stackoverflow answer for bit shifting behaviour in c#: http://stackoverflow.com/questions/9210373/why-do-shift-operations-always-result-in-a-signed-int-when-operand-is-32-bits
        private const uint availableBlockNumbers = (uint)((1L << 32) - reservedBlockNumbers); // 32-bit block numbers
        private const uint metaDataBlocks = 1; // Number of blocks used for meta data (doesn't include the FAT)
        private const uint blockSize = 1 << 12; // 4KB blocks
        public const ulong globalMaxFSSize = (ulong)availableBlockNumbers * blockSize + (1L << 32) * 4 + metaDataBlocks * blockSize;
        //numBlocks * (blockSize + fatEntrySize) + metaDataSize. FAT size is rounded up to a whole number of blocks, assuming reservedBlockNumbers < blockSize/4.
        public const uint fileEntrySize = 1 << 8; // 256B
        private const uint fatEntriesPerBlock = blockSize / 4;
        public const uint filesEntriesPerBlock = blockSize / fileEntrySize;

        private const int freeBlocksOffset = 12;
        private const int firstFreeBlockOffset = 16;

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
            NewFSWriteFAT();
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
            InitRootFolder();
            InitSearchFile();

            ReadFAT();

            initialized = true;

            Console.WriteLine("Root dir spans {0} blocks", FileNumberOfBlocks(rootDirBlock));
            Console.WriteLine("Search file spans {0} blocks", FileNumberOfBlocks(searchFileBlock));
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
        /// Update the meta-data field "first free block".
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
        /// <param name="data">Data to write to JCDVFS-file.</param>
        public void Write(ulong offset, byte[] data)
        {
            fs.Seek((long)offset, SeekOrigin.Begin);
            bw.Write(data);
        }

        /// <summary>
        /// Write data to JCDVFS-file.
        /// </summary>
        /// <param name="offset">Absolute offset in to JCDVFS-file at which to start writing data.</param>
        /// <param name="data">Data to write to JCDVFS-file.</param>
        public void Write(ulong offset, ushort data)
        {
            fs.Seek((long)offset, SeekOrigin.Begin);
            bw.Write(data);
        }

        /// <summary>
        /// Write data to JCDVFS-file.
        /// </summary>
        /// <param name="offset">Absolute offset in to JCDVFS-file at which to start writing data.</param>
        /// <param name="data">Data to write to JCDVFS-file.</param>
        public void Write(ulong offset, uint data)
        {
            fs.Seek((long)offset, SeekOrigin.Begin);
            bw.Write(data);
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
            ulong offset = (ulong)FatOffset(index);
            // Console.WriteLine("Set fat[{0}] = 0x{1} (0x{2})", index, value.ToString("X"), offset.ToString("X"));
            Write(offset, value);
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
            FatSet(index, freeBlock);
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
            return (dataOffsetBlocks + dataBlock) * blockSize + blockOffset;
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

        /// <summary>
        /// Round up integer divison
        /// </summary>
        /// <param name="num">Numerator</param>
        /// <param name="den">Denominator</param>
        /// <returns></returns>
        private ulong ruid(ulong num, ulong den)
        {
            return (num + den - 1) / den;
        }

        private void NewFSSetSize(ulong size)
        {
            // Adjust size so that it is a multiple of block size.
            size = ruid(size, blockSize); // Round up to whole blocks.
            size -= metaDataBlocks; // Without metadata blocks.
            ulong sizeMultiple = 1 + fatEntriesPerBlock; // One FAT block + number of data blocks it represents.
            fatBlocks = (uint)ruid(size, sizeMultiple); // Number of FAT blocks.
            fat = new uint[fatBlocks * fatEntriesPerBlock];

            InitSize(true);

            // These are written in newFSWriteMetaData()

            // The current number of unused blocks in the FS.
            // The JCDVFS-file might not yet be big enough to actually hold this many blocks!
            freeBlocks = fatBlocks * fatEntriesPerBlock - firstFreeBlock;
            firstFreeBlock = 2; // The first free block is after the (initially empty) root dir and search file block.

            fs.SetLength((long)currentSize);
        }

        private void NewFSWriteFAT()
        {
            // Not using fatSet in this function because of performance.
            fs.Seek(FatOffset(0), SeekOrigin.Begin);
            for (uint i = 0; i < this.fatBlocks * fatEntriesPerBlock; i += 1)
            {
                if (fat[i] == 0)
                {
                    bw.Write(freeBlock);
                }
                else
                {
                    bw.Write(fat[i]);
                }
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
            currentSize = currentNumBlocks * blockSize;
            maxSize = maxNumBlocks * blockSize;
        }

        private void ReadFAT()
        {
            // The FAT placed contiously, starting from the first data block.
            fs.Seek(metaDataBlocks * blockSize, SeekOrigin.Begin);
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
            fs.Seek(0L, SeekOrigin.Begin);
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
            fs.Seek(0L, SeekOrigin.Begin);

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
            // The FAT is updated in createRootFolder, which is why that's not done here.
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
        /// Traverse the FAT to figure out how many blocks a file spans.
        /// </summary>
        /// <param name="firstBlock"></param>
        /// <returns></returns>
        private uint FileNumberOfBlocks(uint firstBlock)
        {
            // File doesn't exist if the first block is free, or if the next block is one of the reserved blocks.
            if (fat[firstBlock] == freeBlock || fat[firstBlock] <= reservedBlockNumbers)
            {
                return 0;
            }

            uint nextBlock = fat[firstBlock];
            uint count = 1;
            while (nextBlock != endOfChain && nextBlock != freeBlock &&
                   nextBlock != rootDirBlock && nextBlock != searchFileBlock)
            {
                nextBlock = fat[nextBlock];
                count += 1;
            }
            return count;
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
            return this.freeBlocks * JCDFAT.blockSize;
        }
    }
}
