using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace vfs.core {
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct JCDDirEntry {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 243)]
        public string name;
        public ulong size;
        public bool isFolder;
        public uint firstBlock;

        public JCDDirEntry fromByteArr(byte[] byteArr) {
            int size = Marshal.SizeOf(typeof(JCDDirEntry));
            if(byteArr.Length != size) {
                throw new InvalidCastException();
            }

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(byteArr, 0, ptr, size);
            JCDDirEntry ret = (JCDDirEntry) Marshal.PtrToStructure(ptr, typeof(JCDDirEntry));
            Marshal.FreeHGlobal(ptr);
            return ret;
        }
    }

    internal class JCDFile {
        protected JCDDirEntry entry;
        /*private string name;
        private ulong size;
        private bool isFolder;
        private uint firstBlock;*/

        protected JCDFAT container;
        protected JCDFolder parent;
        protected ulong parentIndex;
        protected string path;

        public static JCDFile fromDirEntry(JCDFAT container, JCDDirEntry entry, JCDFolder parent, ulong parentIndex, string path) {
            if(entry.isFolder) {
                return new JCDFolder(container, entry, parent, parentIndex, path);
            }
            else {
                return new JCDFile(container, entry, parent, parentIndex, path);
            }
        }

        protected JCDFile(JCDFAT container, JCDDirEntry entry, JCDFolder parent, ulong parentIndex, string path) {
            this.entry = entry;
            /*name = entry.name;
            size = entry.size;
            isFolder = entry.isFolder;
            firstBlock = entry.firstBlock;*/

            this.container = container;
            this.parent = parent;
            this.parentIndex = parentIndex;
            this.path = path;
        }
    }
}
