using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using vfs.core.visitor;
using vfs.exceptions;
using vfs.common;

namespace vfs.core {
    // We are suppressing a lot of messages on this type. This is because we care deeply
    // about the precise byte-size of it.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct JCDDirEntry {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 120)]
        // Name and Size should be updated on an instance of JCDFile if
        // you want them to be updated automatically on disk.
        public string Name; // 240B
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public ulong Size; // 8B
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public bool IsFolder; // 4B
        internal uint FirstBlock; // 4B

        internal static JCDDirEntry FromByteArr(byte[] byteArr) {
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

        internal static int StructSize() {
            return Marshal.SizeOf(typeof(JCDDirEntry));   
        }

        internal bool IsEmpty()
        {
            return (Name.Length == 0 && IsFolder);
        }

        internal bool IsFinal()
        {
            return (Name.Length == 0 && !IsFolder);
        }

        public static bool operator ==(JCDDirEntry d1, JCDDirEntry d2) {
            return (d1.Name == d2.Name &&
                    d1.Size == d2.Size &&
                    d1.FirstBlock == d2.FirstBlock &&
                    d1.IsFolder == d2.IsFolder);
        }

        public static bool operator !=(JCDDirEntry d1, JCDDirEntry d2) {
            return !(d1 == d2);
        }

        public override bool Equals(object obj) {
            if (!(obj is JCDDirEntry)) {
                return false;
            }
            return this == (JCDDirEntry)obj;
        }

        public override int GetHashCode() {
            return (int)FirstBlock; // FirstBlock should be unique when the code behaves correctly.
        }
    }

    internal class JCDFile {
        protected JCDDirEntry entry;
        protected JCDFAT container;
        protected JCDFolder parent;
        protected uint parentIndex;
        protected string path;
        protected uint level;

        internal string Name {
            get { return this.entry.Name; }
            set {
                var oldPath = this.path;

                // Update name
                this.entry.Name = value;
                this.UpdateEntryOnParent(this.entry);

                // Update path
                this.path = Helpers.PathGetDirectoryName(this.path) + value;
                
                // Update file index
                container.OnFileMoved(oldPath, Path);
            }
        }

        internal ulong Size
        {
            get { return this.entry.Size; }
            set {
                this.entry.Size = value;
                this.UpdateEntryOnParent(this.entry);
            }
        }

        internal bool IsFolder
        {
            get { return this.entry.IsFolder; }
        }

        internal JCDDirEntry Entry { get { return this.entry; } }
        internal string Path { get { return this.path; } }
        internal JCDFolder Parent { get { return this.parent; } }
        internal uint Level { get { return this.level; } }

        //internal static byte[] EmptyEntry = { 0x00, 0xFF }; // 0x00FF
        //internal static byte[] FinalEntry = { 0x00, 0x00 }; // 0x0000

        internal static JCDFile FromDirEntry(JCDFAT container, JCDDirEntry entry, JCDFolder parent, uint parentIndex, string path, uint level) {
            if(entry.IsFolder) {
                return new JCDFolder(container, entry, parent, parentIndex, path, level);
            }
            else {
                return new JCDFile(container, entry, parent, parentIndex, path, level);
            }
        }

        protected JCDFile(JCDFAT container, JCDDirEntry entry, JCDFolder parent, uint parentIndex, string path, uint level) {
            if(parent != null) {
                if(String.IsNullOrEmpty(entry.Name) && (entry.Size != 0 || entry.FirstBlock != 0)) {
                    throw new InvalidFileNameException();
                }

                //if(!Helpers.PathIsValid(path, entry.IsFolder))
                if(!Helpers.FileNameIsValid(entry.Name)) {
                    throw new InvalidFileNameException();
                }
            }

            this.entry = entry;
            this.container = container;
            this.parent = parent;
            this.parentIndex = parentIndex;
            this.path = path;
            this.level = level;
        }

        /// <summary>
        /// Delete file and all potential subdirectories.
        /// Subdirectories are deleted in a depth-first manner.
        /// </summary>
        internal void Delete(bool skipEntryDeletion)
        {
            //Don't want to delete empty entries...
            if(EntryIsEmpty() || EntryIsFinal()) {
                return;
            }

            // If this is a folder, delete all dir entries recursively.
            if (entry.IsFolder)
            {
                var files = ((JCDFolder)this).GetFileEntries();
                foreach (var file in files)
                {
                    file.Delete(true);
                }
            }

            // Delete blocks for this file.
            // All (potential) sub-entries will have been deleted at this point.
            container.WalkFATChain(entry.FirstBlock, new BlockFreerVisitor());

            // Remove this dir-entry from parent folder.
            if(!skipEntryDeletion) {
                DeleteEntry();

                container.tryShrink();
            }
            container.OnFileDeleted(Path);
        }
        internal void DeleteEntry() {
            parent.DeleteEntry((uint) parentIndex);
        }

        internal bool EntryIsEmpty()
        {
            return this.entry.IsEmpty();
        }

        internal bool EntryIsFinal()
        {
            return this.entry.IsFinal();
        }

        protected uint GetLastBlockId()
        {
            var blockVisitor = (LastBlockIdVisitor)container.WalkFATChain(entry.FirstBlock, new LastBlockIdVisitor());
            return blockVisitor.Block;
        }

        private void UpdateEntryOnParent(JCDDirEntry dirEntry)
        {
            if (this.parent == null)
            {
                return;
            }
            this.parent.setEntry(this.parentIndex, dirEntry);
        }

        /// <summary>
        /// Expand file by some amount of bytes.
        /// </summary>
        /// <param name="expandBytes">Bytes to expand file by.</param>
        /// <returns>FAT index of the first block that was allocated.</returns>
        internal uint ExpandBytes(long expandBytes) {
            if ((ulong)expandBytes > container.FreeSpace()) {
                throw new NotEnoughSpaceException();
            }

            if (expandBytes == 0) {
                return GetLastBlockId();
            }

            var blocksToAllocate = Math.Max(1, Helpers.ruid(expandBytes, JCDFAT.blockSize));

            // Do one FAT chaining here because we want to save firstNewBlock.
            uint prevBlock = GetLastBlockId();
            var firstNewBlock = container.GetFreeBlock();
            var nextBlock = firstNewBlock;
            container.FatSet(prevBlock, nextBlock);
            prevBlock = nextBlock;

            // Chain remaining FAT blocks.
            for (int i = 0; i < blocksToAllocate - 1; i += 1) {
                nextBlock = container.GetFreeBlock();
                container.FatSet(prevBlock, nextBlock);
                prevBlock = nextBlock;
                // Clear the newly allocated block in case it has old data.
                container.ZeroBlock(nextBlock);
            }
            container.FatSetEOC(prevBlock);

            if (this.IsFolder) {
                // Folders always span exactly the amount of blocks they have allocated.
                this.Size += (ulong)(blocksToAllocate * JCDFAT.blockSize);
            }
            else {
                // Files don't necessarily span the full amount of blocks they have allocated.
                this.Size += (ulong)expandBytes;
            }

            container.OnFileResized(Path, (long)this.Size);
            return firstNewBlock;
        }

        /// <summary>
        /// Shrink file by `shrinkBytes`.
        /// If `shrinkBytes` is higher than the size of the file, the file will be truncated to 0 bytes.
        /// </summary>
        /// <param name="shrinkBytes">Amount of bytes to shrink file by.</param>
        internal void ShrinkBytes(long shrinkBytes) {
            if (shrinkBytes <= 0) {
                return;
            }

            if (this.Size - (ulong)shrinkBytes <= 0) {
                shrinkBytes = (long)this.Size;
            }

            var newSize = this.Size - (ulong)shrinkBytes;
            long currentNumBlocks = Math.Max(1, Helpers.ruid((long)this.Size, JCDFAT.blockSize));
            long newNumBlocks = (long)Math.Max(1, Helpers.ruid(newSize, JCDFAT.blockSize));
            if (currentNumBlocks > newNumBlocks) {
                var blockVisitor = new NthBlockIdVisitor(newNumBlocks + 1);
                blockVisitor = (NthBlockIdVisitor)container.WalkFATChain(entry.FirstBlock, blockVisitor);
                container.WalkFATChain(blockVisitor.Block, new BlockFreerVisitor());
            }

            this.Size -= (ulong)shrinkBytes;
            container.OnFileResized(Path, (long)this.Size);
        }

        /// <summary>
        /// Expand folder by one block.
        /// </summary>
        /// <returns>FAT index of newly allocated block.</returns>
        internal uint ExpandOneBlock() {
            return ExpandBytes(JCDFAT.blockSize);
        }

        public void SetDirectoryPath(string directoryPath) {
            this.path = Helpers.PathCombine(directoryPath, this.Name);
        }

        public JCDFAT GetContainer() {
            return this.container;
        }
    }
}
