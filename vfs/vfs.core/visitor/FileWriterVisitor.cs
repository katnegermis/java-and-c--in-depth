using System;
using vfs.core;

namespace vfs.core.visitor
{
    class FileWriterVisitor : IVisitor
    {
        /// <summary>
        /// Returns the offset into the buffer where the file continues
        /// </summary>
        /// <returns>The offset of next block of data in the buffer.</returns>
        public delegate uint BufferIndex();

        private BufferIndex f;
        private long remainingFileSize;
        private byte[] buffer;

        public FileWriterVisitor(long fileSize, byte[] buffer, BufferIndex f)
        {
            this.f = f;
            remainingFileSize = fileSize;
            this.buffer = buffer;
        }

        public bool Visit(JCDFAT vfs, uint block)
        {
            ulong vfsOffset = vfs.BlockGetByteOffset(block, 0);
            uint bufferPos = f();

            int writeBytes = (int) Math.Min((uint) JCDFAT.blockSize, (uint) remainingFileSize);
            vfs.Write(vfsOffset, buffer, (int) bufferPos, writeBytes);
            remainingFileSize -= JCDFAT.blockSize;

            return true;
        }
    }
}
