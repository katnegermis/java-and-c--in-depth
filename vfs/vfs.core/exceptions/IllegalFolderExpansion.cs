using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace vfs.core.exceptions {
    [Serializable]
    public class IllegalFolderExpansion : Exception {
        public IllegalFolderExpansion() : base() { }
        public IllegalFolderExpansion(string msg) : base(msg) { }
        public IllegalFolderExpansion(string msg, Exception e) : base(msg, e) { }
        protected IllegalFolderExpansion(SerializationInfo si, StreamingContext sc) : base(si, sc) { }
    }
}
