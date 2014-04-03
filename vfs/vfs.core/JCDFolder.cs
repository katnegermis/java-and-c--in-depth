using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using vfs.core.visitor;

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
                Name = null, Size = 0, IsFolder = true, FirstBlock = JCDFAT.rootDirBlock
            };
            return new JCDFolder(vfs, entry, null, 0, null);
        }

        public static JCDFolder createRootFolder(JCDFAT vfs) {
            JCDFolder root = rootFolder(vfs);
            vfs.FatSetEOC(JCDFAT.rootDirBlock);
            root.setEntryFinal(0);
            return root;
        }

        private ulong entryOffset(uint index) {
            return container.FileGetByteOffset(entry.FirstBlock, index / JCDFAT.filesEntriesPerBlock,
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

        /// <summary>
        /// Get list of dir entries read from firstBlock and continuing in the FAT chain.
        /// </summary>
        /// <param name="firstBlock"></param>
        /// <returns></returns>
        public List<JCDDirEntry> GetDirEntries(uint firstBlock)
        {
            var dirEntries = new List<JCDDirEntry>();

            // Get the contents of a block and create dir entries from it.
            container.WalkFATChain(firstBlock, new FileReaderVisitor(src => {
                for (int i = 0; i < JCDFAT.fatEntriesPerBlock; i += 1)
                {
                    int size = JCDDirEntry.StructSize();
                    var dst = new byte[size];
                    Buffer.BlockCopy(src, i * size, dst, 0, size);
                    var entry = JCDDirEntry.FromByteArr(dst);
                    // Decide whether the entry was the last entry. If it was, we probably want
                    // to return false (meaning that we don't want the contents of the next block.)
                    //if (entry.IsLastEntryOfThisFolder_PhewThisIsALongFunctionName_OhWell_ItsCSharp())
                    //{
                    //return false;
                    //}
                    dirEntries.Add(entry);
                }
                return true;
            }));

            return dirEntries;
        }

        public void populate() {
            //TODO: implement (we could probably copy most of this from our AOS code)
        }
    }
}
