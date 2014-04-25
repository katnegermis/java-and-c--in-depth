using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vfs.exceptions;

namespace vfs.core {
    public class JCDFileStream : Stream {
        private JCDFile file;
        private long currentNumBlocks;

        // Implementations.
        private long length;
        override public long Length {
            get {
                return length;
            }
        }

        private long position;
        override public long Position {
            get {
                return position;
            }
            set {
                this.position = value;
            }
        }

        // Dummy implementations
        override public bool CanRead { get { return true; } }
        override public bool CanSeek { get { return true; } }
        override public bool CanTimeout { get { return true; } }
        override public bool CanWrite { get { return true; } }
        override public int ReadTimeout { get { return 0; } }
        override public int WriteTimeout { get { return 0; } }

        internal JCDFileStream(JCDFile file) {
            this.file = file;
            this.length = (long)file.Size;
            this.currentNumBlocks = (long)Helpers.ruid(file.Size + 1, JCDFAT.blockSize);
            this.position = 0L;
        }

        override public void Flush() {
            // Will be flushed when the file system is closed.
        }

        public override int Read(byte[] buffer, int offset, int count) {
            // Make sure that offset and count don't go beyond bound of file
            if (offset + count > length) {
                throw new Exception("File is not that big!");
            }
            if (buffer.Length < count) {
                throw new Exception("Buffer is not big enough!");
            }
            var vfs = file.GetContainer();
            vfs.ReadFile(buffer, (ulong)(position + offset), (ulong)count, file.Entry.FirstBlock);
            position += offset + count;
            return buffer.Length;
        }

        public override void Write(byte[] data, int offset, int count) {
            // Make sure that we don't try to write more data than we received.
            if (data.Length < count) {
                count = data.Length;
            }

            var vfs = file.GetContainer();
            // Increase file length in case we're writing beyond #blocks currently allocated.
            long allocatedBytes = this.currentNumBlocks * JCDFAT.blockSize;
            long requiredBytes = this.position + offset + count;
            if (requiredBytes > allocatedBytes) {
                long expandBytes = requiredBytes - allocatedBytes;

                // Make sure that there's enough free space on the vfs.
                if ((ulong)expandBytes > vfs.FreeSpace()) {
                    throw new NotEnoughSpaceException();
                }

                // Allocate required blocks.
                long extraBlocksRequired = Helpers.ruid(expandBytes, JCDFAT.blockSize);
                for (int i = 0; i < extraBlocksRequired; i += 1) {
                    // TODO: could be implemented more efficiently by doing multiple blocks at the time.
                    file.ExpandOneBlock();
                }

                // Update size of file.
                file.Size = (ulong)(allocatedBytes + expandBytes);
                currentNumBlocks += extraBlocksRequired;
            }

            // Update the number of bytes the file spans (which could be less than the
            // number of blocks * block size.)
            if ((ulong)requiredBytes > file.Size) {
                file.Size = (ulong)requiredBytes;
            }

            length = (long)file.Size;

            vfs.WriteFile(data, position + offset, file.Entry.FirstBlock);
            position += offset + count;
        }

        public override long Seek(long offset, SeekOrigin origin) {
            switch (origin) {
                case SeekOrigin.Begin:
                    position = offset;
                    break;
                case SeekOrigin.Current:
                    position += offset;
                    break;
                case SeekOrigin.End:
                    position = length + offset;
                    // We wait to set the length until user writes beyond file length.
                    break;
            }
            return position;
        }

        public override void SetLength(long value) {
            this.length = value;
        }

        public override void Close() {
            base.Close();
        }

        public JCDFAT GetVFS() {
            return file.GetContainer();
        }
    }
}
