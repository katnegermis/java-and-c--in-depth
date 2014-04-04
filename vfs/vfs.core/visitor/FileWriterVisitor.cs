using System;
using vfs.core;

namespace vfs.core.visitor
{
    class FileWriterVisitor : IVisitor
    {
        /// <summary>
        /// Returns contents of file to write, one block at a time.
        /// </summary>
        /// <returns>The next block of data to write, or less if end of file is reached.</returns>
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

            int writeBytes = Math.Min((int) JCDFAT.blockSize, (int) remainingFileSize);
            vfs.Write(vfsOffset, buffer, (int) bufferPos, writeBytes);
            remainingFileSize -= JCDFAT.blockSize;

            return true;
        }
    }
}
