using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using vfs.core.visitor;

namespace vfs.core {
    [StructLayout(LayoutKind.Sequential, Pack=1, CharSet = CharSet.Unicode)]
    public struct JCDDirEntry {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 120)]
        public string Name; // 240B
        public ulong Size; // 8B
        public bool IsFolder; // 4B
        public uint FirstBlock; // 4B

        public static JCDDirEntry FromByteArr(byte[] byteArr) {
            int size = StructSize();
            Console.WriteLine("JCDDirEntry struct has size: {0}", size);
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
            var dst = new byte[2];
            Buffer.BlockCopy(Name.ToCharArray(), 0, dst, 0, 2);

            return dst == JCDFile.EmptyEntry;
        }

        public bool IsFinal()
        {
            Console.WriteLine("File name length of JCDDirEntry: {0}", Name.Length);
            var dst = new byte[2];
            Buffer.BlockCopy(Name.ToCharArray(), 0, dst, 0, 2);
            return dst == JCDFile.FinalEntry;
        }
    }

    internal class JCDFile {
        protected JCDDirEntry entry;
        protected JCDFAT container;
        protected JCDFolder parent;
        protected uint parentIndex;
        protected string path;

        public static byte[] EmptyEntry = { 0x00, 0xFF }; // 0x00FF
        public static byte[] FinalEntry = { 0x00, 0x00 }; // 0x0000

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

        /// <summary>
        /// Delete file and all potential subdirectories.
        /// Subdirectories are deleted in a depth-first manner.
        /// </summary>
        public void Delete()
        {
            // If this is a folder, delete all dir entries recursively.
            if (entry.IsFolder)
            {
                var files = ((JCDFolder)this).GetFileEntries();
                foreach (var file in files)
                {
                    file.Delete();
                }
            }

            // Delete blocks for this file.
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

        public bool EntryIsFinal()
        {
            return this.entry.IsFinal();
        }

        protected uint GetLastBlockId()
        {
            var blockVisitor = (LastBlockIdVisitor)container.WalkFATChain(entry.FirstBlock, new LastBlockIdVisitor());
            return blockVisitor.Block;
        }


        /// <summary>
        /// Expand file by one block.
        /// </summary>
        /// <returns>FAT index of newly allocated block.</returns>
        protected uint ExpandOneBlock()
        {
            var prevLastBlock = GetLastBlockId();
            var newLastBlock = container.GetFreeBlock();

            // Update FAT entries.
            container.FatSet(prevLastBlock, newLastBlock);
            container.FatSetEOC(newLastBlock);

            // Update the file's current size.
            // Make sure to reflect this change on disk.
            entry.Size += JCDFAT.blockSize;
            return newLastBlock;
        }
    }
}
