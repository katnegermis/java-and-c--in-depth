using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vfs.core.visitor
{
    class LastBlockIdVisitor : IVisitor
    {
        public uint Block;

        public bool Visit(JCDFAT vfs, uint block)
        {
            this.Block = block;
            return true;
        }
    }
}
