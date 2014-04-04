using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using vfs.core.visitor;

namespace vfs.core {
    internal class JCDFolder : JCDFile {
        private bool populated = false;
        private List<JCDFile> entries;
        private uint firstEmptyEntry;

        public JCDFolder(JCDFAT container, JCDDirEntry entry, JCDFolder parent, uint parentIndex, string path)
            : base(container, entry, parent, parentIndex, path) {
            entries = new List<JCDFile>();
        }

        public static JCDFolder rootFolder(JCDFAT vfs) {
            var entry = new JCDDirEntry {
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
            // Assuming any corresponding entry in the entries List is already set
            int size = Marshal.SizeOf(entry);
            byte[] byteArr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(entry, ptr, false);
            Marshal.Copy(ptr, byteArr, 0, size);
            Marshal.FreeHGlobal(ptr);

            container.Write(entryOffset(index), byteArr);
        }

        /// <summary>
        /// Mark an entry as Empty.
        /// </summary>
        /// <param name="index">Index of the entry to be marked.</param>
        public void setEntryEmpty(uint index) {
            container.Write(entryOffset(index), emptyEntry);

            // Update firstEmptyEntry if we just freed an 'earlier' one.
            if (index < this.firstEmptyEntry)
            {
                this.firstEmptyEntry = index;
            }
        }

        /// <summary>
        /// Mark an entry as the final entry in the folder.
        /// </summary>
        /// <param name="index">Index of the entry to be marked.</param>
        public void setEntryFinal(uint index) {
            container.Write(entryOffset(index), finalEntry);
        }

        /// <summary>
        /// Get list of dir entries read from firstBlock and continuing in the FAT chain.
        /// </summary>
        /// <param name="firstBlock"></param>
        /// <returns></returns>
        private List<JCDDirEntry> GetDirEntries()
        {
            var dirEntries = new List<JCDDirEntry>();

            // Get the contents of a block and create dir entries from it.
            container.WalkFATChain(this.entry.FirstBlock, new FileReaderVisitor(src =>
            {
                for (int i = 0; i < JCDFAT.fatEntriesPerBlock; i += 1)
                {
                    int size = JCDDirEntry.StructSize();
                    var dst = new byte[size];
                    Buffer.BlockCopy(src, i * size, dst, 0, size);
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
        
        /// <summary>
        /// Add JCDDirEntry to folder.
        /// </summary>
        /// <param name="dirEntry">Entry to be added.</param>
        /// <returns>Index of the newly added entry.</returns>
        public uint AddDirEntry(JCDDirEntry dirEntry)
        {
            uint index = GetEmptyEntryIndex();
            var entryPath = Helpers.PathCombine(this.path, dirEntry.Name);
            this.entries.Insert((int)index, JCDFile.FromDirEntry(container, dirEntry, this, index, entryPath));
            setEntry(index, dirEntry);
            return index;
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
            var dirEntries = GetDirEntries();
            for (uint i = 0; i < dirEntries.Count; i += 1)
            {
                var dirEntry = dirEntries[(int)i];
                var entryPath = FileGetPath(dirEntry.Name);
                this.entries.Add(JCDFile.FromDirEntry(this.container, dirEntry, this, i, entryPath));

                // Set firstEmptyEntry if not already set.
                if (!emptyEntrySet && dirEntry.IsEmpty())
                {
                    this.firstEmptyEntry = i;
                    emptyEntrySet = true;
                }
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
            // Check whether firstEmptyEntry is still empty. If not, update it.
            if (!this.entries[(int)this.firstEmptyEntry].EntryIsEmpty())
            {
                for (int i = (int)this.firstEmptyEntry + 1; i < this.entries.Count; i += 1)
                {
                    if (this.entries[i].EntryIsEmpty())
                    {
                        this.firstEmptyEntry = (uint)i;
                        return this.firstEmptyEntry;
                    }
                }
            }
            // There were no more empty entries! Allocate block so that we can store more entries.
            uint newBlock = this.Expand();

            // TODO: Create empty dirEntries and write them to newBlock.
            // Add the new (empty) dirEntries to this.entries.
            // Set this.firstEmptyEntry to the first index of the new block.
            // How do we know which index the first entry in the new block has?
            return 0;
        }

        /// <summary>
        /// Get the path of a child of this folder.
        /// Usually used when JCDFiles are initialized.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public string FileGetPath(JCDFile file)
        {
            // Check whether dirEntry is in entries. If it is not
            if (!entries.Contains(file))
            {
                // TODO: Throw proper exception.
                throw new Exception("That file is not a child of this folder!");
            }
            return Helpers.PathCombine(this.path, file.GetName());
        }

        public string FileGetPath(string name)
        {
            foreach (var entry in entries)
            {
                if (entry.GetName() == name)
                {
                    return Helpers.PathCombine(this.path, name);
                }
            }
            // TODO: Throw proper exception.
            throw new Exception("A file with that name is not a child of this folder!");
        }
    }
}
