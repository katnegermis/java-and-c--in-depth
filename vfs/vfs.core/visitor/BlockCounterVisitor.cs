using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vfs.core.visitor
{
    /// <summary>
    /// Nice for debugging.
    /// </summary>
    class BlockCounterVisitor : IVisitor
    {
        public uint Blocks = 0;
        private bool debug = false;

        public BlockCounterVisitor(bool debug)
        {
            this.debug = debug;
        }

        public BlockCounterVisitor()
        {
        }

        public bool Visit(JCDFAT vfs, uint block)
        {
            if (this.debug)
            {
                Console.WriteLine("Walked block: {0}", block);
            }
            this.Blocks += 1;
            return true;
        }
    }
}
