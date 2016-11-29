using System;

using ConsoleManager;

using Open.Nat;

namespace PokeD.Server.Desktop
{
    public static partial class Program
    {
        private static bool ExecuteCommand(string message)
        {
            var command = message.Remove(0, 1).ToLower();
            message = message.Remove(0, 1);

            if (command.StartsWith("stop"))
            {
                FastConsole.Stop();

                Server?.Stop();

                NatDiscoverer.ReleaseAll();
                Console.WriteLine("Stopped the server. Press any key to continue...");
                Console.ReadKey();
                Environment.Exit((int) ExitCodes.Success);
            }

            else if (command.StartsWith("clear"))
                FastConsole.ClearOutput();

            else
                return Server.ExecuteServerCommand(message);

            return true;
        }
    }
}
