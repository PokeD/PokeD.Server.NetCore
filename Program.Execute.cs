using System;

using ConsoleManager;

#if OPENNAT
using Open.Nat;
#endif

namespace PokeD.Server.Desktop
{
    public static partial class Program
    {
        private static bool ExecuteCommand(string message)
        {
            var command = message.Remove(0, 1).ToLower();
            message = message.Remove(0, 1);

            if (message.StartsWith("stop"))
            {
                FastConsole.Stop();

                Server?.Stop();
 #if OPENNAT
                NatDiscoverer.ReleaseAll();
#endif
                Console.WriteLine("Stopped the server. Press any key to continue$(SolutionDir).");
                Console.ReadKey();
                Environment.Exit((int) ExitCodes.Success);
            }

            else if (message.StartsWith("clear"))
                FastConsole.ClearOutput();

            else if (command.StartsWith("help server"))
                return Server.ExecuteCommand(message.Remove(0, 11));

            else if (command.StartsWith("help"))
                return ExecuteHelpCommand(message.Remove(0, 4));

            else
                return Server.ExecuteCommand(message);

            return true;
        }

        private static bool ExecuteHelpCommand(string command) { return false; }
    }
}
