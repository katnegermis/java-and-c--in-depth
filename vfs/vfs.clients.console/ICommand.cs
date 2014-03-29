using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace console.client
{

    public interface ICommand
    {
        int Execute(VFSConsole console);
    }
}
