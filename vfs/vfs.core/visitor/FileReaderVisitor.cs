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

        private GetFileContents f;
        private ulong bytesLeft;

        public FileReaderVisitor(ulong size, GetFileContents f)
        {
            this.f = f;
            bytesLeft = size;
        }

        public bool Visit(JCDFAT vfs, uint block)
        {
            ulong vfsOffset = vfs.BlockGetByteOffset(block, 0);

            var bytesToRead = (uint) Math.Min(bytesLeft, (ulong)JCDFAT.blockSize);
            bytesLeft -= bytesToRead;
            // Pass contents of block on to f and inform caller whether f wants 
            // the contents of the next block.
            return f(vfs.Read(vfsOffset, bytesToRead), bytesLeft == 0);
        }
    }
}
