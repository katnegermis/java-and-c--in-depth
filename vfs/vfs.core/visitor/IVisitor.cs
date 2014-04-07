using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vfs.core.visitor
{
    internal interface IVisitor
    {
        bool Visit(JCDFAT vfs, uint block);
    }
}
