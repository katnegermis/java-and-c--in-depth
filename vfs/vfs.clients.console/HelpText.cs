using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace console.client
{
    class HelpText
    {

        private HelpText()
        {

        }

        public static void Show()
        {
            Console.WriteLine("");
            Console.WriteLine("The functions that can be called all the time are:");
            Console.WriteLine("help \t\t\t\t Show help text");
            Console.WriteLine("exit \t\t\t\t Exit the client");
            Console.WriteLine("");

            Console.WriteLine("The functions that can be called only when not mounted:");
            Console.WriteLine("create \t path size \t\t Create a new VFS");
            Console.WriteLine("delete \t path \t\t\t Delete the VFS");
            Console.WriteLine("open \t path \t\t\t Open the VFS");
            Console.WriteLine("");

            Console.WriteLine("The functions that can be called only when mounted:");
            Console.WriteLine("close \t\t\t\t Close the opened VFS");
            Console.WriteLine("ls \t [path] \t\t List the files/dirs in the current or given directory");
            Console.WriteLine("cd \t path \t\t\t Change to the given directory");
            Console.WriteLine("rm \t [-r] path  \t\t Remove the given file/dir (recursively if set)");
            Console.WriteLine("mk \t [-p] path size \t Make a new file of size (and parents if set)");
            Console.WriteLine("mkdir \t [-p] path \t\t Make a new directory (and parents if set)");
            Console.WriteLine("mv \t -hv/vh/vv source target Move the source to the target");
            Console.WriteLine("cp \t -hv/vh/vv source target Copy the source to the target");
            Console.WriteLine("\t\t\t\t -hv is import, -vh export, -vv in the VFS");
            Console.WriteLine("rn \t path newName \t\t Rename the file/dir");
            Console.WriteLine("size \t\t\t\t Show the size of the VFS");
            Console.WriteLine("free \t\t\t\t Show the free space");
            Console.WriteLine("occupied \t\t\t Show the occupied space");
            Console.WriteLine("search \t[-i] filename\t\t Search current directory for file");
            Console.WriteLine("\t\t\t\t  -i for case insensitive, -a search from root, -n non recursive search");
            Console.WriteLine("");
        }

    }
}
