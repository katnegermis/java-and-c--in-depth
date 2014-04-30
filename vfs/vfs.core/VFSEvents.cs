using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vfs.core {
    public class VFSEvents {
        public delegate void MoveFileEventHandler(string oldPath, string newPath);
        public delegate void DeleteFileEventHandler(string path);
        public delegate void AddFileEventHandler(string path);
    }
}
