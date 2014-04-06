using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vfs.core.visitor
{
    class FileDeleterVisitor : IVisitor
    {
        public bool Visit(JCDFAT vfs, uint block)
        {
            // Make sure that we don't delete any reserved blocks.
            // This should probably be fixed somewhere else, but I guess
            // this is a nice safety mechanism to begin with.
            if (block < JCDFAT.reservedBlockNumbers) {
                return true;
            }

            vfs.FatSetFree(block);
            return true;
        }
    }
}
