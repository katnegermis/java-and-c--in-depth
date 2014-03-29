using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace console.client
{
    class Parser
    {
        public static ICommand Parse(string commandString) { 
         // Parse your string and create Command object
         var commandParts = commandString.Split(' ').ToList();
         var commandName = commandParts[0];
         var args = commandParts.Skip(1).ToList(); // the arguments is after the command
         switch(commandName)
         {
             // Create command based on CommandName (and maybe arguments)
             case "exit":
                 return new VFSConsole.ExitCommand();
             case "quit":
                 return new VFSConsole.ExitCommand();
             case "help":
                 return new VFSConsole.HelpCommand();
             case "create":
                 return new VFSConsole.CreateCommand(args);
             case "delete":
                 return new VFSConsole.DeleteCommand(args);
             case "open":
                 return new VFSConsole.OpenCommand(args);
             case "close":
                 return new VFSConsole.CloseCommand();
             case "ls":
                 return new VFSConsole.LsCommand();
             case "cd":
                 return new VFSConsole.CdCommand(args);
             case "rm":
                 return new VFSConsole.RmCommand(args);
            case "mk":
                 return new VFSConsole.MkCommand(args);
            case "mkdir":
               return new VFSConsole.MkdirCommand(args);
            case "mv":
                return new VFSConsole.MvCommand(args);
             case "rn":
                 return new VFSConsole.RnCommand(args);
            case "free":
                return new VFSConsole.FreeCommand();
            case "occupied":
                return new VFSConsole.OccupiedCommand();
             default:
                 return new VFSConsole.NULLCommand();
         }
    }

    }
}
