using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using vfs.core.visitor;

namespace vfs.core {
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct JCDDirEntry {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 243)]
        public string Name;
        public ulong Size;
        public bool IsFolder;
        public uint FirstBlock;

        public static JCDDirEntry FromByteArr(byte[] byteArr) {
            int size = StructSize();
            if(byteArr.Length != size) {
                throw new InvalidCastException();
            }

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(byteArr, 0, ptr, size);
            var ret = (JCDDirEntry)Marshal.PtrToStructure(ptr, typeof(JCDDirEntry));
            Marshal.FreeHGlobal(ptr);
            return ret;
        }

        public static int StructSize() {
            return Marshal.SizeOf(typeof(JCDDirEntry));   
        }

        public bool IsEmpty()
        {
            // TODO: Implement.
            return false;
        }

        public bool IsFinal()
        {
            // TODO: Implement.
            return false;
        }
    }

    internal class JCDFile {
        protected const ushort emptyEntry = 0x00FF;
        protected const ushort finalEntry = 0x0000;

        protected JCDDirEntry entry;
        protected JCDFAT container;
        protected JCDFolder parent;
        protected uint parentIndex;
        protected string path;

        public static JCDFile FromDirEntry(JCDFAT container, JCDDirEntry entry, JCDFolder parent, uint parentIndex, string path) {
            if(entry.IsFolder) {
                return new JCDFolder(container, entry, parent, parentIndex, path);
            }
            else {
                return new JCDFile(container, entry, parent, parentIndex, path);
            }
        }

        protected JCDFile(JCDFAT container, JCDDirEntry entry, JCDFolder parent, uint parentIndex, string path) {
            this.entry = entry;
            this.container = container;
            this.parent = parent;
            this.parentIndex = parentIndex;
            this.path = path;
        }

        public void Delete()
        {
            // If this is a folder, delete all dir entries recursively.
            if (entry.IsFolder)
            {
                var folder = (JCDFolder)this;
                var files = folder.GetFileEntries();

                foreach (var file in files)
                {
                    file.Delete();
                }
            }

            // Delete blocks for this file, whether folder or file.
            // All (potential) sub-entries will have been deleted at this point.
            container.WalkFATChain(entry.FirstBlock, new FileDeleterVisitor());

            // Remove this dir-entry from parent folder.
            parent.DeleteEntry((uint)parentIndex);
        }

        public string GetName()
        {
            return entry.Name;
        }

        public bool EntryIsEmpty()
        {
            return this.entry.IsEmpty();
        }

        protected uint GetLastBlockId()
        {
            var blockVisitor = (LastBlockIdVisitor)container.WalkFATChain(entry.FirstBlock, new LastBlockIdVisitor());
            return blockVisitor.Block;
        }
    }
}
