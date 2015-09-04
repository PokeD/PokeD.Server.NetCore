using System;
using System.Diagnostics;
using System.Threading;

using PokeD.Core.Wrappers;

using PokeD.Server.Windows.Extensions;
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
            try
            {
                AppDomain.CurrentDomain.UnhandledException += (sender, e) => FatalExceptionObject(e.ExceptionObject);
            }
            catch (Exception exception)
            {
                // Maybe it will cause a recursive exception.
                if (Server != null)
                    Server.Stop();

                FatalExceptionHandler(exception);
            }
            Start(args);
        }

        private static void Start(string[] args)
        {
            foreach (var arg in args)
            {
                if (arg.StartsWith("-enableconsole"))
                    ConsoleManager.Start();
            }

            Server = new Server();
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

                    var time = (int) (10 - watch.ElapsedMilliseconds);
                    if (time < 0) time = 0;
                    Thread.Sleep(time);
                }

                watch.Reset();
                watch.Start();
            }
        }


        private static void FatalExceptionObject(object exceptionObject)
        {
            var huh = exceptionObject as Exception ?? new NotSupportedException("Unhandled exception doesn't derive from System.Exception: " + exceptionObject);
            FatalExceptionHandler(huh);
        }

        private static void FatalExceptionHandler(Exception exception)
        {
            LogManager.WriteLine(exception.GetExceptionDetails());
        }
    }
}
