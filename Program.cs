using System;
using System.Diagnostics;
using System.Threading;
using PokeD.Core.Data;
using PokeD.Core.Packets.Chat;
using PokeD.Core.Wrappers;
using PokeD.Server.Windows.WrapperInstances;

namespace PokeD.Server.Windows
{
    public static class Program
    {
        public static int MainThreadTime { get; set; }

        static Program()
        {
            FileSystemWrapper.Instance = new FileSystemWrapperInstance();
            NetworkTCPClientWrapper.Instance = new NetworkTCPClientWrapperInstance();
            NetworkTCPServerWrapper.Instance = new NetworkTCPServerWrapperInstance();
            InputWrapper.Instance = new InputWrapperInstance();
            ThreadWrapper.Instance = new ThreadWrapperInstance();
        }

        public static Server Server { get; set; }

        public static void Main(string[] args)
        {
            Server = new Server();

            ConsoleManager.Start();

            Update();
        }

        public static void Update()
        {
            var watch = Stopwatch.StartNew();
            while (true)
            {
                Server.Update();

                if (watch.ElapsedMilliseconds < 16)
                {
                    var time = (int) (16 - watch.ElapsedMilliseconds);
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
