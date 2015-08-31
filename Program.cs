using System;
using System.Diagnostics;
using System.Threading;

using PokeD.Core.Wrappers;

using PokeD.Server.Windows.WrapperInstances;

namespace PokeD.Server.Windows
{
    public static partial class Program
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
                Server = server ?? new Server();
            

            Server.Start();

            Update();
        }

        public static long MainThreadTime { get; private set; }
        private static void Update()
        {
            var watch = Stopwatch.StartNew();
            while (true)
            {
                if (ConsoleManager.InputAvailable)
                {
                    var input = ConsoleManager.ReadLine();

                    if (input.StartsWith("/"))
                    {
                        ConsoleManager.Clear();
                        ConsoleManager.WriteLine(input);
                        ExecuteCommand(input);
                    }
                }

                if(Server != null)
                    Server.Update();
                else
                    return;
                

                if (watch.ElapsedMilliseconds < 10)
                {
                    MainThreadTime = watch.ElapsedMilliseconds;
                    Thread.Sleep((int)(10 - watch.ElapsedMilliseconds));
                }

                watch.Reset();
                watch.Start();
            }
        }
    }
}
