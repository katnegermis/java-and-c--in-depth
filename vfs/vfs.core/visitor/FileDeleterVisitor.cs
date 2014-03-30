using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vfs.core.visitor
{
    class FileDeleterVisitor : IVisitor
    {
        public void Visit(JCDFAT vfs, uint block)
        {
            vfs.FatSetFree(block);
        }
    }
}
