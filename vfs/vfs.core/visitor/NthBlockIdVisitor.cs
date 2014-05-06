using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vfs.core.visitor {
    class NthBlockIdVisitor : IVisitor {
        public uint Block;
        private long blockNumbersLeft;

        public NthBlockIdVisitor(long blockNumber) {
            this.blockNumbersLeft = blockNumber;
        }

        public bool Visit(JCDFAT vfs, uint block) {
            this.blockNumbersLeft -= 1;
            if (this.blockNumbersLeft <= 0) {
                this.Block = block;
                return false;
            }
            return true;
        }
    }
}
