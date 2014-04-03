using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using vfs.core.visitor;

namespace vfs.core {
    internal class JCDFolder : JCDFile {
        private bool populated = false;
        private List<JCDFile> entries;

        public JCDFolder(JCDFAT container, JCDDirEntry entry, JCDFolder parent, uint parentIndex, string path)
            : base(container, entry, parent, parentIndex, path) {
            entries = new List<JCDFile>();
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
                    // Decide whether the entry was the last entry. If it was, we don't want 
                    // to read the contents of the next block.
                    if (entry.IsFinalEntry())
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

        public void AddFile(JCDDirEntry dirEntry)
        {
            uint index = GetFreeIndex();
            var entryPath = Helpers.PathCombine(this.path, dirEntry.Name);
            this.entries.Insert((int)index, JCDFile.FromDirEntry(container, dirEntry, this, index, entryPath));
            // Actually write dir entry to disk
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
        /// Read JCDDirEntries from disk.
        /// </summary>
        public void Populate() {
            var dirEntries = GetDirEntries(entry.FirstBlock);
            for (uint i = 0; i < dirEntries.Count; i += 1)
            {
                var dirEntry = dirEntries[(int)i];
                var entryPath = FileGetPath(dirEntry.Name);
                this.entries.Add(JCDFile.FromDirEntry(this.container, dirEntry, this, i, entryPath));
            }
            this.populated = true;
        }

        /// <summary>
        /// Get the first empty entry of the folder.
        /// Allocates another block if there are no more entries left in the currently allocated blocks.
        /// </summary>
        /// <returns></returns>
        public uint GetFreeIndex()
        {
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
