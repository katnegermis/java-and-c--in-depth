using System;
using vfs.common;

namespace vfs.core.indexing {
    [Serializable()]
    public class IndexedFile {
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
