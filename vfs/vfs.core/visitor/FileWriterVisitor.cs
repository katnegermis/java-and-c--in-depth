using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vfs.common;

namespace vfs.core.visitor {
    class FileWriterVisitor : IVisitor {
        private byte[] data;
        private int remainingBytes;
        private long blocksTraversed;
        private long startingBlock;
        private uint blockOffset;

        public FileWriterVisitor(byte[] data, long offset) {
            this.data = data;
            this.remainingBytes = data.Length;
            this.blocksTraversed = 0;
            this.startingBlock = offset / JCDFAT.blockSize; // Floor division.
            // This is always >= 0 since the statement above uses floor division.
            // It also fits in to a uint since the block size is only 2^12.
            // blockOffset is only used for the first block we have to write to,
            // as all other blocks will be written from their first byte.
            this.blockOffset = (uint)(offset - startingBlock * JCDFAT.blockSize);
        }

        public bool Visit(JCDFAT vfs, uint block) {
            // Don't write anything until we reach the first block we want to write.
            if (blocksTraversed < startingBlock) {
                blocksTraversed += 1;
                return true;
            }

            var vfsOffset = vfs.BlockGetByteOffset(block, blockOffset);
            var bytesToWrite = (int)Math.Min(remainingBytes, JCDFAT.blockSize - blockOffset);
            blockOffset = 0; // We only need the blockOffset for the first block.
            vfs.Write(vfsOffset, data, data.Length - remainingBytes, bytesToWrite);
            remainingBytes -= bytesToWrite;

            blocksTraversed += 1;
            return true;
        }
    }
}
