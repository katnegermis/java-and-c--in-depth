using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vfs.common;
using vfs.core;

namespace vfs.core.indexing {
    [Serializable()]
    public class IndexedFile {
        // Name isn't _really_ necessary since we use it as key.
        public string Name;
        public string Path;

        public IndexedFile(string path) {
            this.Name = Helpers.PathGetFileName(path); ;
            this.Path = path;
        }

        public override bool Equals(object obj) {
            if (obj == null || !(obj is IndexedFile)) {
                return false;
            }
            return this.Name == ((IndexedFile)obj).Name && this.Path == ((IndexedFile)obj).Path;
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }

        public static bool operator ==(IndexedFile f1, IndexedFile f2) {
            return f1.Equals(f2);
        }

        public static bool operator !=(IndexedFile f1, IndexedFile f2) {
            return !(f1 == f2);
        }
    }
}
