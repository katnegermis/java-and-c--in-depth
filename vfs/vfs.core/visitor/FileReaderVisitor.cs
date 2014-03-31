using System;
using vfs.core;

namespace vfs.core.visitor
{
    class FileReaderVisitor : IVisitor
    {
        public delegate void GetFileContents(byte[] data);

        private GetFileContents f;

        public FileReaderVisitor(GetFileContents f)
        {
            this.f = f;
        }

        public bool Visit(JCDFAT vfs, uint block)
        {
            ulong vfsOffset = vfs.BlockGetByteOffset(block, 0);
            // Pass contents of block on to f.
            f(vfs.Read(block, JCDFAT.blockSize));
            return true;
        }
    }
}
