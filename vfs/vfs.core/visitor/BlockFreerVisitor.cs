using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vfs.exceptions;

namespace vfs.core.visitor
{
    class BlockFreerVisitor : IVisitor
    {
        public bool Visit(JCDFAT vfs, uint block)
        {
            // Make sure that we don't delete any reserved blocks.
            // This should probably be fixed somewhere else, but I guess
            // this is a nice safety mechanism to begin with.
            if (block < JCDFAT.reservedBlockNumbers) {
                throw new RootDeletionException();
            }

            vfs.FatSetFree(block);
            return true;
        }
    }
}
