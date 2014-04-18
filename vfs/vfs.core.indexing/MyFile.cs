using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vfs.core.indexing {
    [Serializable()]
    public class MyFile {
        // Name isn't really necessary since we use it as key.
        public string Name;
        public string Path;

        public MyFile(string name, string path) {
            this.Name = name;
            this.Path = path;
        }

        public override bool Equals(object obj) {
            if (obj == null || !(obj is MyFile)) {
                return false;
            }
            return this.Name == ((MyFile)obj).Name && this.Path == ((MyFile)obj).Path;
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }

        public static bool operator ==(MyFile f1, MyFile f2) {
            return f1.Equals(f2);
        }

        public static bool operator !=(MyFile f1, MyFile f2) {
            return !(f1 == f2);
        }
    }
}
