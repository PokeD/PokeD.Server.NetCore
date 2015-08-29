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

        public static Server Server { get; set; }

        public static void Main(string[] args)
        {
            Server = new Server();

            Update();
        }

        public static void Update()
        {
            var watch = Stopwatch.StartNew();
            while (true)
            {
                Server.Update();

                if (watch.ElapsedMilliseconds < 1000)
                {
                    var time = (int) (1000 - watch.ElapsedMilliseconds);
                    //Console.WriteLine(time);
                    Thread.Sleep(time);
                }

                watch.Reset();
                watch.Start();
            }
        }
    }
}
