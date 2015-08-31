using System;
using System.Diagnostics;
using System.Threading;

using PokeD.Core.Wrappers;

using PokeD.Server.Windows.WrapperInstances;

namespace PokeD.Server.Windows
{
    public static class Program
    {
        static Program()
        {
            FileSystemWrapper.Instance = new FileSystemWrapperInstance();
            NetworkTCPClientWrapper.Instance = new NetworkTCPClientWrapperInstance();
            NetworkTCPServerWrapper.Instance = new NetworkTCPServerWrapperInstance();
            InputWrapper.Instance = new InputWrapperInstance();
            ThreadWrapper.Instance = new ThreadWrapperInstance();
        }

        static Server Server { get; set; }

        public static void Main(string[] args)
        {
            ConsoleManager.Start();

            Server server;
            if (!FileSystemWrapper.LoadSettings(Server.FileName, out server))
            {
                ConsoleManager.WriteLine("Error: Server.json is invalid. Please fix it or delete.");
                Console.ReadLine();
                return;
            }
            else
                Server = server;
            
            Server.Start();

            Update();
        }

        public static int MainThreadTime { get; private set; }
        private static void Update()
        {
            var watch = Stopwatch.StartNew();
            while (true)
            {
                if (ConsoleManager.InputAvailable)
                {
                    var input = ConsoleManager.ReadLine();

                    if (input.StartsWith("/stop"))
                    {
                        Server.Stop();
                        ConsoleManager.WriteLine("Stopped the server. Press Enter to continue.");
                        Console.ReadLine();
                        return;
                    }

                    else if(input.StartsWith("/say "))
                        Server.SendGlobalChatMessageToAll(input.Remove(0, 5));

                    else if (input.StartsWith("/message "))
                        Server.SendGlobalChatMessageToAll(input.Remove(0, 9));

                    else if (input.StartsWith("/"))
                        Server.ExecuteServerCommand(input.Remove(0, 1));
                }

                Server.Update();

                if (watch.ElapsedMilliseconds < 100)
                {
                    var time = (int) (100 - watch.ElapsedMilliseconds);
                    if (time < 0)
                        time = 0;

                    MainThreadTime = (int) watch.ElapsedMilliseconds;
                    Thread.Sleep(time);
                }

                watch.Reset();
                watch.Start();
            }
        }
    }
}
