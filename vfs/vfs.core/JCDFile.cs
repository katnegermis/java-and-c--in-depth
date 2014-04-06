﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using vfs.core.visitor;
using vfs.exceptions;

namespace vfs.core {
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct JCDDirEntry {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 120)]
        // Name and Size should be updated on an instance of JCDFile if
        // you want them to be updated automatically on disk.
        public string Name; // 240B
        public ulong Size; // 8B
        public bool IsFolder; // 4B
        public uint FirstBlock; // 4B

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
            return (Name.Length == 0 && IsFolder);
        }

        public bool IsFinal()
        {
            return (Name.Length == 0 && !IsFolder);
        }
    }

    internal class JCDFile {
        protected JCDDirEntry entry;
        protected JCDFAT container;
        protected JCDFolder parent;
        protected uint parentIndex;
        protected string path;
        protected uint level;

        public string Name {
            get { return this.entry.Name; }
            set {
                this.entry.Name = value;
                this.UpdateEntry(this.entry);
            }
        }

        public ulong Size
        {
            get { return this.entry.Size; }
            set {
                this.entry.Size = value;
                this.UpdateEntry(this.entry);
            }
        }

        public bool IsFolder
        {
            get { return this.entry.IsFolder; }
        }

        public JCDDirEntry Entry { get { return this.entry; } }
        public string Path { get { return this.path; } }
        public JCDFolder Parent { get { return this.parent; } }
        public uint Level { get { return this.level; } }

        //public static byte[] EmptyEntry = { 0x00, 0xFF }; // 0x00FF
        //public static byte[] FinalEntry = { 0x00, 0x00 }; // 0x0000

        public static JCDFile FromDirEntry(JCDFAT container, JCDDirEntry entry, JCDFolder parent, uint parentIndex, string path, uint level) {
            if(entry.IsFolder) {
                return new JCDFolder(container, entry, parent, parentIndex, path, level);
            }
            else {
                return new JCDFile(container, entry, parent, parentIndex, path, level);
            }
        }

        protected JCDFile(JCDFAT container, JCDDirEntry entry, JCDFolder parent, uint parentIndex, string path, uint level) {
            if(entry.Name == "" && (entry.Size != 0 || entry.FirstBlock != 0)) {
                throw new InvalidFileNameException();
            }

            //if(!Helpers.PathIsValid(path, entry.IsFolder))
            if(!Helpers.FileNameIsValid(entry.Name))
            {
                throw new InvalidFileNameException();
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
        public void Delete(bool skipEntryDeletion)
        {
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
            container.WalkFATChain(entry.FirstBlock, new FileDeleterVisitor());

            // Remove this dir-entry from parent folder.
            if(!skipEntryDeletion) {
                DeleteEntry();

                container.tryShrink();
            }
        }
        public void DeleteEntry() {
            parent.DeleteEntry((uint) parentIndex);
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

        private void UpdateEntry(JCDDirEntry entry)
        {
            if (this.parent == null)
            {
                return;
            }
            this.parent.setEntry(this.parentIndex, this.entry);
        }
    }
}
