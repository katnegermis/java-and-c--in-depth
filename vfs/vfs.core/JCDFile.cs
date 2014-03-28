using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vfs.core
{
    class JCDFile
    {
        // I think it might be a good idea to align this to, say, 256B so that we can fit
        // 16 file descriptors in a cluster. This means that the file name can be up to ~254B.
        private string name;
        private ulong size;
        private bool isFolder;
        private ulong firstCluster;
    }
}
