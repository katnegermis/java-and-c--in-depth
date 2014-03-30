using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace vfs.core {
    internal class JCDFolder : JCDFile {
        private const ushort emptyEntry = 0x00FF;
        private const ushort finalEntry = 0x0000;

        private bool populated = false;
        private LinkedList<JCDFile> entries;

        public JCDFolder(JCDFAT container, JCDDirEntry entry, JCDFolder parent, ulong parentIndex, string path)
            : base(container, entry, parent, parentIndex, path) {
            entries = new LinkedList<JCDFile>();
        }

        public static JCDFolder rootFolder(JCDFAT vfs) {
            JCDDirEntry entry = new JCDDirEntry {
                name = null, size = 0, isFolder = true, firstBlock = JCDFAT.rootDirBlock
            };
            return new JCDFolder(vfs, entry, null, 0, null);
        }

        public static JCDFolder createRootFolder(JCDFAT vfs) {
            JCDFolder root = rootFolder(vfs);
            vfs.fatSetEOC(JCDFAT.rootDirBlock);
            root.setEntryFinal(0);
            return root;
        }

        private ulong entryOffset(uint index) {
            return container.fileGetByteOffset(entry.firstBlock, index / JCDFAT.filesEntriesPerBlock,
                (index % JCDFAT.filesEntriesPerBlock) * JCDFAT.fileEntrySize);
        }

        public void setEntry(uint index, JCDDirEntry entry) {
            //Assuming any corresponding entry in the entries LinkedList is already set

            int size = Marshal.SizeOf(entry);
            byte[] byteArr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(entry, ptr, false);
            Marshal.Copy(ptr, byteArr, 0, size);
            Marshal.FreeHGlobal(ptr);

            container.Write(entryOffset(index), byteArr);
        }

        public void setEntryEmpty(uint index) {
            container.Write(entryOffset(index), emptyEntry);
        }
        public void setEntryFinal(uint index) {
            container.Write(entryOffset(index), finalEntry);
        }

        public void populate() {
            //TODO: implement (we could probably copy most of this from our AOS code)
        }
    }
}
