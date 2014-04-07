using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using vfs.core.visitor;
using vfs.exceptions;

namespace vfs.core {
    internal class JCDFolder : JCDFile {

        private bool populated = false;
        private List<JCDFile> entries;

        // This variable is for internal use only! Use GetEmptyEntryIndex if you want the correct value.
        private uint firstEmptyEntry;

        public JCDFolder(JCDFAT container, JCDDirEntry entry, JCDFolder parent, uint parentIndex, string path, uint level)
            : base(container, entry, parent, parentIndex, path, level) {
            entries = new List<JCDFile>();
        }

        public static JCDFolder rootFolder(JCDFAT vfs) {
            var blockCounter = (BlockCounterVisitor)vfs.WalkFATChain(JCDFAT.rootDirBlock, new BlockCounterVisitor());
            var entry = new JCDDirEntry {
                Name = null, Size = blockCounter.Blocks * JCDFAT.blockSize, IsFolder = true, FirstBlock = JCDFAT.rootDirBlock
            };
            return new JCDFolder(vfs, entry, null, 0, "/", 0);
        }

        public static JCDFolder createRootFolder(JCDFAT vfs) {
            vfs.FatSetEOC(JCDFAT.rootDirBlock);
            JCDFolder root = rootFolder(vfs);
            root.setEntryFinal(0);
            return root;
        }

        private ulong entryOffset(uint index) {
            return container.FileGetByteOffset(this.entry.FirstBlock, index / JCDFAT.dirEntriesPerBlock,
                (index % JCDFAT.dirEntriesPerBlock) * JCDFAT.dirEntrySize);
        }

        public void setEntry(uint index, byte[] byteArr)
        {

            // Check that the given index goes at most one block beyond the number of blocks currently allocated.
            var numBlocks = Helpers.ruid(this.entry.Size, JCDFAT.blockSize);
            var blocksRequired = Helpers.ruid(index + 1, JCDFAT.dirEntriesPerBlock);
            if (blocksRequired > numBlocks + 1)
            {
                // TODO: Throw proper exception.
                throw new Exception("Folders are only allowed to expand by one block at a time!");
            }

            // Expand folder if `index` points beyond the folder's currently allocated blocks.
            if (blocksRequired > numBlocks)
            {
                this.ExpandOneBlock();
                //setEntryFinal(index + 1);
            }

            container.Write(entryOffset(index), byteArr);
            //Console.WriteLine("Wrote entry for '{0}' on disk.", JCDDirEntry.FromByteArr(byteArr).Name);
        }

        public void setEntry(uint index, JCDDirEntry entry) {
            // Assuming any corresponding entry in the entries List is already set
            int size = JCDDirEntry.StructSize();
            byte[] byteArr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(entry, ptr, false);
            Marshal.Copy(ptr, byteArr, 0, size);
            Marshal.FreeHGlobal(ptr);
            setEntry(index, byteArr);     
        }

        private void setFileEntry(uint index, JCDFile file) {
            //index must not be > Count, i.e. not more than 1 beyond the end of entries
            if(index < this.entries.Count) {
                entries[(int)index] = file;

                // Update firstEmptyEntry if we mark an 'earlier' entry empty.
                if(file.EntryIsEmpty() && index < firstEmptyEntry) {
                    firstEmptyEntry = index;
                }
            }
            else {
                entries.Add(file);

                if(firstEmptyEntry == index) {
                    firstEmptyEntry++;
                }
            }
        }

        /// <summary>
        /// Mark an entry as Empty.
        /// </summary>
        /// <param name="index">Index of the entry to be marked.</param>
        public void setEntryEmpty(uint index) {
            setFileEntry(index, emptyFile(index, false));

            var emptyEntry = new JCDDirEntry { Name = "", Size = 0, IsFolder = true, FirstBlock = 0 };
            setEntry(index, emptyEntry);
        }

        /// <summary>
        /// Mark an entry as the final entry in the folder.
        /// </summary>
        /// <param name="index">Index of the entry to be marked.</param>
        public void setEntryFinal(uint index) {
            setFileEntry(index, emptyFile(index, true));

            var finalEntry = new JCDDirEntry { Name = "", Size = 0, IsFolder = false, FirstBlock = 0 };
            setEntry(index, finalEntry);
        }

        private JCDFile emptyFile(uint index, bool isFinal) {
            var entry = new JCDDirEntry {Name = "", Size = 0, IsFolder = !isFinal, FirstBlock = 0};
            return JCDFile.FromDirEntry(this.container, entry, this, index, this.path, level + 1);
        }

        /// <summary>
        /// Get list of dir entries read from firstBlock and continuing in the FAT chain.
        /// </summary>
        /// <param name="firstBlock"></param>
        /// <returns></returns>
        private List<JCDDirEntry> GetDirEntries(uint firstBlock)
        {
            var dirEntries = new List<JCDDirEntry>();

            // Get the contents of a block and create dir entries from it.
            // The function defined below is called once for each block in the folder.
            container.WalkFATChain(firstBlock, new FileReaderVisitor(this.Size, (blockData, lastBlock) =>
            {
                int size = JCDDirEntry.StructSize();
                var entriesInBlock = Math.Min(JCDFAT.dirEntriesPerBlock, blockData.Length / JCDFAT.dirEntrySize);
                for (int i = 0; i < entriesInBlock; i += 1)
                {
                    var dst = new byte[size];
                    Buffer.BlockCopy(blockData, i * size, dst, 0, size);
                    var entry = JCDDirEntry.FromByteArr(dst);

                    // If this is final entry we don't want to read the contents of the next block.
                    // In fact, there should be no more blocks to read.
                    if (entry.IsFinal())
                    {
                        return false;
                    }
                    dirEntries.Add(entry);
                }
                return true;
            }));

            return dirEntries;
        }

        public List<JCDFile> GetFileEntries()
        {
            if (!this.populated)
            {
                this.Populate();
            }
            return this.entries;
        }

        public JCDFile GetFile(string name)
        {
            if (!this.populated)
            {
                this.Populate();
            }
            foreach (var file in this.entries)
            {
                if (file.Name == name)
                {
                    return file;
                }
            }
            return null;
        }
        
        /// <summary>
        /// Add JCDDirEntry to folder.
        /// </summary>
        /// <param name="dirEntry">Entry to be added.</param>
        /// <returns>Index of the newly added entry.</returns>
        public JCDFile AddDirEntry(JCDDirEntry dirEntry)
        {
            // Verify that a file with that name doesn't already exist.
            if (this.GetFile(dirEntry.Name) != null)
            {
                throw new FileAlreadyExistsException();
            }
            uint index = GetEmptyEntryIndex();
            var entryPath = FileGetPath(dirEntry.Name, dirEntry.IsFolder);
            var newFile = JCDFile.FromDirEntry(container, dirEntry, this, index, entryPath, level + 1);
            setFileEntry(index, newFile);
            setEntry(index, dirEntry);
            return newFile;
        }

        /// <summary>
        /// Delete dir entry from folder structure.
        /// </summary>
        /// <param name="index"></param>
        public void DeleteEntry(uint index)
        {
            this.setEntryEmpty(index);
        }

        /// <summary>
        /// Populate this.entries list with JCDFiles.
        /// </summary>
        private void Populate() {
            bool emptyEntrySet = false;
            var dirEntries = GetDirEntries(this.entry.FirstBlock);
            for (uint i = 0; i < dirEntries.Count; i += 1)
            {
                var dirEntry = dirEntries[(int)i];
                var entryPath = FileGetPath(dirEntry.Name, dirEntry.IsFolder);
                var newFile = JCDFile.FromDirEntry(this.container, dirEntry, this, i, entryPath, level + 1);
                this.entries.Add(newFile);

                // Set firstEmptyEntry if not already set.
                if (!emptyEntrySet && (dirEntry.IsEmpty() || dirEntry.IsFinal()))
                {
                    this.firstEmptyEntry = i;
                    emptyEntrySet = true;
                }
            }
            if (!emptyEntrySet)
            {
                this.firstEmptyEntry = (uint)dirEntries.Count;
            }
            this.populated = true;
        }

        /// <summary>
        /// Get the first empty entry index of the folder.
        /// Allocates another block if there are no more entries left in the currently allocated blocks.
        /// </summary>
        /// <returns></returns>
        public uint GetEmptyEntryIndex()
        {
            if (!this.populated)
            {
                this.Populate();
            }

            if (this.firstEmptyEntry >= this.entries.Count)
            {
                return this.firstEmptyEntry;
            }

            var firstEmpty = this.entries[(int)this.firstEmptyEntry];

            if (firstEmpty.EntryIsEmpty() || firstEmpty.EntryIsFinal())
            {
                return this.firstEmptyEntry;
            }

            int i;
            for (i = (int)firstEmptyEntry + 1; i < this.entries.Count; i += 1)
            {
                if (this.entries[i].EntryIsEmpty())
                {
                    this.firstEmptyEntry = (uint)i;
                }
            }
            this.firstEmptyEntry = (uint)i;
            this.setEntryEmpty((uint)i);
            return this.firstEmptyEntry;
        }

        /// <summary>
        /// Whether the give file is equal to, os a descendant of, this folder.
        /// Usually used when JCDFiles are initialized.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public bool IsParentOf(JCDFile file) {
            if(level > file.Level) {
                return false;
            }
            JCDFile rec;
            for(rec = file; level < rec.Level; rec = rec.Parent);
            return (rec == this);
        }

        /// <summary>
        /// Get the path of a child of this folder.
        /// Usually used when JCDFiles are initialized.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        /*public string FileGetPath(JCDFile file)
        {
            // Check whether dirEntry is in entries. If it is not
            return Helpers.PathCombine(this.path, file.Name);
        }*/

        public string FileGetPath(string name, bool isFolder)
        {
            /*if(isFolder) {
                return new Uri(this.path, name + "/");
            }
            else {
                return new Uri(this.path, name);
                
            }*/

            if(isFolder) {
                return Helpers.PathCombine(this.path, name) + "/";
            }
            else {
                return Helpers.PathCombine(this.path, name);

            }
            
            // TODO: Throw proper exception.
            throw new Exception("A file with that name is not a child of this folder!");
        }

        /// <summary>
        /// Expand folder by one block.
        /// </summary>
        /// <returns>FAT index of newly allocated block.</returns>
        protected uint ExpandOneBlock()
        {
            var prevLastBlock = GetLastBlockId();
            var newLastBlock = container.GetFreeBlock();

            // Update FAT entries.
            container.FatSet(prevLastBlock, newLastBlock);
            container.FatSetEOC(newLastBlock);

            // Clear the newly allocated block in case it has old data.
            container.ZeroBlock(newLastBlock);

            // Update the file's current size.
            // Make sure to reflect this change on disk.
            this.Size += JCDFAT.blockSize;
            return newLastBlock;
        }
    }
}
