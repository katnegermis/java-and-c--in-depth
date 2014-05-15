using System;
using vfs.core;

namespace vfs.core.visitor
{
    class FileReaderVisitor : IVisitor
    {
        /// <summary>
        /// Called with contents of file, one block at a time.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>True if next block is wanted, false otherwise</returns>
        public delegate bool GetFileContents(byte[] data, bool lastBlock);

        public int BytesRead;
        private GetFileContents func;
        private ulong bytesLeft;
        private ulong firstBlockIndex;
        private uint blockOffset;
        private ulong blocksTraversed;

        public FileReaderVisitor(ulong size, GetFileContents f)
        {
            Initialize(size, 0L, f);
        }

        public FileReaderVisitor(ulong size, ulong offset, GetFileContents f) {
            Initialize(size, offset, f);
        }

        private void Initialize(ulong size, ulong fileOffset, GetFileContents f) {
            this.BytesRead = -1;
            this.blocksTraversed = 0;
            this.func = f;
            bytesLeft = size;
            this.firstBlockIndex = fileOffset / JCDFAT.blockSize; // Floor division.
            // blockOffset is always 0 <= x <= JCDFAT.blockSize because of floor division above.
            this.blockOffset = (uint)(fileOffset - firstBlockIndex * JCDFAT.blockSize);
        }

        public bool Visit(JCDFAT vfs, uint block)
        {
            if (blocksTraversed < firstBlockIndex) {
                blocksTraversed += 1;
                return true;
            }

            ulong vfsOffset = vfs.BlockGetByteOffset(block, blockOffset);
            var bytesToRead = (uint)Math.Min(bytesLeft, (ulong)JCDFAT.blockSize - blockOffset);
            BytesRead += (int)bytesToRead;

            // Only needed for the first block. All other blocks are read from the beginning.
            blockOffset = 0;

            bytesLeft -= bytesToRead;
            
            blocksTraversed += 1;

            // Pass contents of block on to f and inform caller whether f wants 
            // the contents of the next block.
            return func(vfs.Read(vfsOffset, bytesToRead), bytesLeft == 0);
        }
    }
}
