using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;

using Aragas.Core.Wrappers;

using PCLStorage;

using PokeD.Server.Desktop.WrapperInstances;

namespace PokeD.Server.Desktop
{
    public static partial class Program
    {
        const string URL = "http://pokemon3d.net/forum/threads/12901/";
        static Server Server { get; set; }


        static Program()
        {
            AppDomainWrapper.Instance = new AppDomainWrapperInstance();
            FileSystemWrapper.Instance = new FileSystemWrapperInstance();
            InputWrapper.Instance = new InputWrapperInstance();

            //LuaWrapper.Instance = new NLuaWrapperInstance();
            LuaWrapper.Instance = new MoonLuaWrapperInstance();

			DatabaseWrapper.Instance = new SQLiteDatabase();
            //DatabaseWrapper.Instance = new CouchbaseDatabase();
            //DatabaseWrapper.Instance = new FileDBDatabase();
			//DatabaseWrapper.Instance = new SiaqodbDatabase();

            TCPClientWrapper.Instance = new TCPClientWrapperInstance();
            TCPListenerWrapper.Instance = new TCPServerWrapperInstance();
            ThreadWrapper.Instance = new ThreadWrapperInstance();
        }

        public static void Main(params string[] args)
        {
            try { AppDomain.CurrentDomain.UnhandledException += (sender, e) => CatchErrorObject(e.ExceptionObject); }
            catch (Exception exception)
            {
                // Maybe it will cause a recursive exception.
                Server?.Stop();

                CatchError(exception);
            }
            Start(args);
        }

        private static void Start(params string[] args)
        {
            foreach (var arg in args)
            {
                if (arg.StartsWith("-enableconsole"))
                    ConsoleManager.Start();

                if (arg.StartsWith("-usenlua"))
                    LuaWrapper.Instance = new NLuaWrapperInstance();

				//if (arg.StartsWith("-usecouchbase"))
				//    DatabaseWrapper.Instance = new CouchbaseDatabase();

                if (arg.StartsWith("-usefiledb"))
                    DatabaseWrapper.Instance = new FileDBDatabase();

                //if (arg.StartsWith("-usensiaqodb"))
                //    DatabaseWrapper.Instance = new SiaqodbDatabase();
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

                if(Server == null || (Server != null && Server.IsDisposing))
                    break;

                Server.Update();



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


        private static void CatchErrorObject(object exceptionObject)
        {
            var exception = exceptionObject as Exception ?? new NotSupportedException("Unhandled exception doesn't derive from System.Exception: " + exceptionObject);
            CatchError(exception);
        }
        private static void CatchError(Exception ex)
        {
            var errorLog = $@"
[CODE]
PokeD.Server Crash Log v {Assembly.GetExecutingAssembly().GetName().Version}
--------------------------------------------------
System specifications:
Operating system: {Environment.OSVersion} [{(Type.GetType("Mono.Runtime") != null ? "Mono" : ".NET")}]
Core architecture: {(Environment.Is64BitOperatingSystem ? "64 Bit" : "32 Bit")}
System language: {CultureInfo.CurrentCulture.EnglishName}
Logical processors: {Environment.ProcessorCount}
{BuildErrorStringRecursive(ex)}
--------------------------------------------------
You should report this error if it is reproduceable or you could not solve it by yourself.
Go To: {URL} to report this crash there.
[/CODE]";

            var crashFile = FileSystemWrapper.CrashLogFolder.CreateFileAsync($"{DateTime.Now:yyyy-MM-dd_HH.mm.ss}.log", CreationCollisionOption.OpenIfExists).Result;
            using (var stream = crashFile.OpenAsync(PCLStorage.FileAccess.ReadAndWrite).Result)
            using (var writer = new StreamWriter(stream))
                writer.Write(errorLog);

#if !DEBUG
            Environment.Exit((int) ExitCodes.UnknownError);
#endif
        }

        private static string BuildErrorStringRecursive(Exception ex)
        {
            return $@"
--------------------------------------------------
Error information:
Type: {ex.GetType().FullName}
Message: {ex.Message}
HelpLink: {(string.IsNullOrWhiteSpace(ex.HelpLink) ? "Empty" : ex.HelpLink)}
Source: {ex.Source}
TargetSite : {ex.TargetSite}
--------------------------------------------------
CallStack:
{ex.StackTrace}
{(ex.InnerException != null ? $@"
--------------------------------------------------
InnerException:
{BuildErrorStringRecursive(ex.InnerException)}" : "")}";
        }
    }
}
