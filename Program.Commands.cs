using System;

namespace PokeD.Server.Windows
{
    public static partial class Program
    {
        private static void ExecuteCommand(string message)
        {
            var command = message.Remove(0, 1).ToLower();
            message = message.Remove(0, 1);

            if (message.StartsWith("stop"))
            {
                Server.Stop();
                ConsoleManager.WriteLine("Stopped the server. Press Enter to continue.");
                Console.ReadLine();
            }

            else if (message.StartsWith("clear"))
                ConsoleManager.Clear();

            else if (command.StartsWith("help server"))
                Server.ExecuteCommand(message.Remove(0, 11));

            else if (command.StartsWith("help"))
                ExecuteHelpCommand(message.Remove(0, 4));

            else
                Server.ExecuteCommand(message);
        }

        private static void ExecuteHelpCommand(string command)
        {

        }

    }
}
