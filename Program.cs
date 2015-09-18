using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

using PCLStorage;

using PokeD.Core.Wrappers;

using PokeD.Server.Windows.Extensions;
using PokeD.Server.Windows.WrapperInstances;

namespace PokeD.Server.Windows
{
    public static partial class Program
    {
        static Server Server { get; set; }


        static Program()
        {
            FileSystemWrapper.Instance = new FileSystemWrapperInstance();
            NetworkTCPClientWrapper.Instance = new NetworkTCPClientWrapperInstance();
            NetworkTCPServerWrapper.Instance = new NetworkTCPServerWrapperInstance();
            InputWrapper.Instance = new InputWrapperInstance();
            ThreadWrapper.Instance = new ThreadWrapperInstance();
        }


        public static void Main(string[] args)
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException += (sender, e) => CatchErrorObject(e.ExceptionObject);
            }
            catch (Exception exception)
            {
                // Maybe it will cause a recursive exception.
                Server?.Stop();

                CatchError(exception);
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
            var exception = exceptionObject as Exception ?? new NotSupportedException("Unhandled exception doesn't derive from System.Exception: " + exceptionObject);
            FatalExceptionHandler(exception);
        }
        private static void FatalExceptionHandler(Exception exception)
        {
            LogManager.WriteLine(exception.GetExceptionDetails());
        }


        private static void CatchErrorObject(object exceptionObject)
        {
            var exception = exceptionObject as Exception ?? new NotSupportedException("Unhandled exception doesn't derive from System.Exception: " + exceptionObject);
            CatchError(exception);
        }
        private static void CatchError(Exception ex)
        {
            var errorLog = string.Format(@"[CODE]
PokeD.Server Crash Log v {0}
--------------------------------------------------
System specifications:
Operating system: {1} [{1}]
Core architecture: {2}
System language: {3}
Logical processors: {4}
--------------------------------------------------
            
Error information:
Message: {5}
InnerException: {6}
HelpLink: {7}
Source: {8}
--------------------------------------------------
CallStack:
{9}
--------------------------------------------------
You should report this error if it is reproduceable or you could not solve it by yourself.
Go To: http://pokemon3d.net/forum/threads/12686/ to report this crash there.
[/CODE]",
                Environment.Version,
                Environment.OSVersion,
                Environment.Is64BitOperatingSystem ? "64 Bit" : "32 Bit",
                CultureInfo.CurrentCulture.EnglishName,
                Environment.ProcessorCount,
                ex.Message,
                ex.InnerException?.Message ?? "Nothing",
                string.IsNullOrWhiteSpace(ex.HelpLink) ? "Nothing" : ex.HelpLink,
                ex.Source,
                ex.InnerException == null ? ex.StackTrace : ex.InnerException.StackTrace + Environment.NewLine + ex.StackTrace);

            var folder = FileSystemWrapper.LogFolder.CreateFolderAsync("Crash", CreationCollisionOption.OpenIfExists).Result;
            var crash = folder.CreateFileAsync($"{DateTime.Now:yyyy-MM-dd_HH.mm.ss}", CreationCollisionOption.OpenIfExists).Result;
            using (var stream = crash.OpenAsync(PCLStorage.FileAccess.ReadAndWrite).Result)
            using (var writer = new StreamWriter(stream))
                writer.Write(errorLog);
        }
    }
}
