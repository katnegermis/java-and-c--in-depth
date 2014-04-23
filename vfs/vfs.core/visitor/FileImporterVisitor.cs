using System;
using vfs.core;

namespace vfs.core.visitor
{
    class FileImporterVisitor : IVisitor
    {
        /// <summary>
        /// Returns the offset into the buffer where the file continues
        /// </summary>
        /// <returns>The offset of next block of data in the buffer.</returns>
        public delegate uint BufferIndex();

        private BufferIndex f;
        private ulong remainingFileSize;
        private byte[] buffer;

        public FileImporterVisitor(ulong fileSize, byte[] buffer, BufferIndex f)
        {
            this.f = f;
            remainingFileSize = fileSize;
            this.buffer = buffer;
        }

        public bool Visit(JCDFAT vfs, uint block)
        {
            ulong vfsOffset = vfs.BlockGetByteOffset(block, 0);
            uint bufferPos = f();

            int writeBytes = (int) Math.Min((ulong) JCDFAT.blockSize, remainingFileSize);
            vfs.Write(vfsOffset, buffer, (int) bufferPos, writeBytes);
            remainingFileSize -= JCDFAT.blockSize;

            return true;
        }
    }
}
