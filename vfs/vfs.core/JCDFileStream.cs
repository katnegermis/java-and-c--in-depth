using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vfs.exceptions;
using vfs.common;

namespace vfs.core {
    public class JCDFileStream : Stream {
        private JCDFile file;
        private ModifyFileEventHandler modifiedCallback;

        // Implementations.
        override public long Length {
            get {
                return (long)file.Size;
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

        internal JCDFileStream(JCDFile file, ModifyFileEventHandler modifiedCallback) {
            this.file = file;
            this.position = 0L;
            this.modifiedCallback = modifiedCallback;
        }

        override public void Flush() {
            // Will be flushed when the file system is closed.
        }

        public override int Read(byte[] buffer, int offset, int count) {
            if (buffer == null || buffer.Length < count) {
                throw new BufferTooSmallException();
            }
            var vfs = file.GetContainer();
            var bytesRead = vfs.ReadFile(buffer, (ulong)(position + offset), (ulong)count, file.Entry.FirstBlock);
            position += offset + count;
            return bytesRead;
        }

        public override void Write(byte[] buffer, int offset, int count) {
            if (buffer == null) {
                throw new BufferTooSmallException("Buffer was null!");
            }

            // Make sure that we don't try to write more data than we received.
            if (buffer.Length < count) {
                count = buffer.Length;
            }

            var vfs = file.GetContainer();
            // Increase file length in case we're writing beyond #blocks currently allocated.
            long requiredBytesTotal = this.position + offset + count;
            if ((ulong)requiredBytesTotal > file.Size) {
                file.ExpandBytes(requiredBytesTotal - (long)file.Size);
            }

            vfs.WriteFile(buffer, position + offset, file.Entry.FirstBlock);
            // Call modified callback.
            // Copy `data` to new place in memory.
            var dataCopy = new byte[count];
            Buffer.BlockCopy(buffer, 0, dataCopy, 0, count);
            modifiedCallback(file.Path, Position, dataCopy);

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
                    position = (long)file.Size + offset;
                    // We wait to set the length until user writes beyond file length.
                    break;
            }
            return position;
        }

        public override void SetLength(long value) {
            if ((ulong)value < file.Size) {
                file.ShrinkBytes(value);
            }
            else {
                file.ExpandBytes(value);
            }
        }

        public override void Close() {
            base.Close();
        }

        public JCDFAT GetVFS() {
            return file.GetContainer();
        }
    }
}
