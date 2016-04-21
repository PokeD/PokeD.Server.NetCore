using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using SystemInfoLibrary.OperatingSystem;
using Aragas.Core.Wrappers;
using ConsoleManager;
using FireSharp;
using FireSharp.Config;

using NDesk.Options;

#if OPENNAT
using Open.Nat;
using System.Linq;
using System.Threading.Tasks;
#endif

using PCLStorage;

using PokeD.Core.Extensions;

using PokeD.Server.Desktop.WrapperInstances;

namespace PokeD.Server.Desktop
{
#if OPENNAT
    public static class TaskExtension
    {
        public static TResult Wait<TResult>(this Task<TResult> task, CancellationTokenSource cancellationTokenSource)
        {
            try
            {
                task.Wait(cancellationTokenSource.Token);
                return task.Result;
            }
            catch(Exception ex) { throw task?.Exception ?? ex; }
        }
    }
#endif

    public static partial class Program
    {
        private struct FBReport
        {
            public string Description;
            public string ErrorCode;
            public DateTime Date;
        }


        private const string REPORTURL = "http://poked.github.io/report/";
        private const string FBURL = "https://poked.firebaseio.com/";
        private static Server Server { get; set; }

#if OPENNAT
        private static bool NATForwardingEnabled { get; set; }
#endif


        static Program()
        {
            AppDomainWrapper.Instance   = new AppDomainWrapperInstance();
            DatabaseWrapper.Instance    = new SQLiteDatabase();
            FileSystemWrapper.Instance  = new FileSystemWrapperInstance();
            InputWrapper.Instance       = new InputWrapperInstance();
            LuaWrapper.Instance         = new MoonLuaWrapperInstance();
            NancyWrapper.Instance       = new NancyWrapperInstance();
            TCPClientWrapper.Instance   = new TCPClientFactoryInstance();
            TCPListenerWrapper.Instance = new TCPServerWrapperInstance();
            ThreadWrapper.Instance      = new ThreadWrapperInstance();
            ConfigWrapper.Instance      = new YamlConfigFactoryInstance();
            
            PacketExtensions.Init();

            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.DefaultConnectionLimit = 100;
        }

        public static void Main(params string[] args)
        {
            try { AppDomain.CurrentDomain.UnhandledException += (sender, e) => CatchErrorObject(e.ExceptionObject); }
            catch (Exception exception)
            {
                var exceptionText = CatchError(exception);
                ReportErrorLocal(exceptionText);
                ReportErrorWeb(exceptionText);
                Stop();
            }

            ParseArgs(args);

            Start();
        }

        private static void ParseArgs(IEnumerable<string> args)
        {
            var options = new OptionSet();
            try
            {
                options = new OptionSet()
                    .Add("c|console", "enables the console.", StartFastConsole)
                    .Add("fps=", "{FPS} of the console, integer.", fps => FastConsole.ScreenFPS = int.Parse(fps))
                    .Add("db|database=", "used {DATABASE_WRAPPER}.", ParseDatabase)
                    .Add("cf|config=", "used {CONFIG_WRAPPER}.", ParseConfig)
#if OPENNAT
                    .Add("n|nat", "enables NAT port forwarding.", str => NATForwardingEnabled = true)
#endif
                    .Add("h|help", "show help.", str => ShowHelp(options));

                options.Parse(args);
            }
            catch (Exception ex) when (ex is OptionException || ex is FormatException)
            {
                FastConsole.Stop();

                Console.Write("PokeD.Server.Desktop: ");
                Console.WriteLine(ex.Message);
                Console.WriteLine("Try `PokeD.Server.Desktop --help' for more information.");

                ShowHelp(options, true);

                Console.WriteLine();
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                Environment.Exit((int) ExitCodes.Success);
            }
        }
        private static void StartFastConsole(string s)
        {
            FastConsole.ConstantAddLine(
                "Main              thread execution time: {0} ms", () => new object[] { MainThreadTime });
            FastConsole.ConstantAddLine(
                "ClientConnections thread execution time: {0} ms", () => new object[] { Server.ClientConnectionsThreadTime });
            FastConsole.ConstantAddLine(
                "PlayerWatcher     thread execution time: {0} ms", () => new object[] { ModuleP3D.PlayerWatcherThreadTime });
            FastConsole.ConstantAddLine(
                "PlayerCorrection  thread execution time: {0} ms", () => new object[] { ModuleP3D.PlayerCorrectionThreadTime });
            FastConsole.ConstantAddLine(
                "ConsoleManager    thread execution time: {0} ms", () => new object[] { FastConsole.ConsoleManagerThreadTime });

            FastConsole.Start();
        }
        private static void ShowHelp(OptionSet options, bool direct = false)
        {
            if (direct)
            {
                Console.WriteLine("Usage: PokeD.Server.Desktop [OPTIONS]");
                Console.WriteLine();
                Console.WriteLine("Options:");

                options.WriteOptionDescriptions(Console.Out);
            }
            else
            {
                FastConsole.WriteLine("Usage: PokeD.Server.Desktop [OPTIONS]");
                FastConsole.WriteLine();
                FastConsole.WriteLine("Options:");

                var opt = new StringWriter();
                options.WriteOptionDescriptions(opt);
                foreach (var line in opt.GetStringBuilder().ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None))
                    FastConsole.WriteLine(line);
            }
        }
        private static void ParseDatabase(string database)
        {
            switch (database.ToLowerInvariant())
            {
                case "nosql":
                case "nosqldb":
                case "file":
                case "filedb":
                case "fdb":
                    DatabaseWrapper.Instance = new FileDBDatabase();
                    break;

                case "sql":
                case "sqldb":
                case "sqlite":
                case "sqlitedb":
                    DatabaseWrapper.Instance = new SQLiteDatabase();
                    break;

                default:
                    throw new FormatException("DATABASE_WRAPPER not correct.");
            }
        }
        private static void ParseConfig(string config)
        {
            switch (config.ToLowerInvariant())
            {
                case "json":
                    ConfigWrapper.Instance = new JsonConfigFactoryInstance();
                    break;

                case "yml":
                case "yaml":
                    ConfigWrapper.Instance = new YamlConfigFactoryInstance();
                    break;

                default:
                    throw new FormatException("CONFIG_WRAPPER not correct.");
            }
        }
        private static void ReportErrorLocal(string exception)
        {
            var crashFile = FileSystemWrapper.CrashLogFolder.CreateFileAsync($"{DateTime.Now:yyyy-MM-dd_HH.mm.ss}.log", CreationCollisionOption.OpenIfExists).Result;
            using (var stream = crashFile.OpenAsync(PCLStorage.FileAccess.ReadAndWrite).Result)
            using (var writer = new StreamWriter(stream))
                writer.Write(exception);
        }
        private static void ReportErrorWeb(string exception)
        {
            if (!Server.AutomaticErrorReporting)
                return;

            var client = new FirebaseClient(new FirebaseConfig { BasePath = FBURL });
            client.Push("", new FBReport
            {
                Description = "Sent from PokeD",
                ErrorCode = exception,
                Date = DateTime.Now
            });
        }

        private static void Start()
        {
            Server = new Server();
            Server.Start();

#if OPENNAT
            NATForwarding();
#endif

            Update();
        }
#if OPENNAT
        private static void NATForwarding()
        {
            if (!NATForwardingEnabled)
                return;

            try
            {
                Logger.Log(LogType.Info, $"Initializing NAT Discovery.");
                var discoverer = new NatDiscoverer();
                Logger.Log(LogType.Info, $"Getting your external IP. Please wait...");
                var device = discoverer.DiscoverDeviceAsync().Wait(new CancellationTokenSource(10000));
                Logger.Log(LogType.Info, $"Your external IP is {device.GetExternalIPAsync().Wait(new CancellationTokenSource(2000))}.");

                foreach (var module in Server.Modules.Where(module => module.Enabled && module.Port != 0))
                {
                    Logger.Log(LogType.Info, $"Forwarding port {module.Port}.");
                    device.CreatePortMapAsync(new Mapping(Protocol.Tcp, module.Port, module.Port, "PokeD Port Mapping")).Wait(new CancellationTokenSource(2000).Token);
                }
            }
            catch (NatDeviceNotFoundException)
            {
                Logger.Log(LogType.Error, $"No NAT device is present or, Upnp is disabled in the router or Antivirus software is filtering SSDP (discovery protocol).");
            }
        }
#endif
        private static void Stop()
        {
            FastConsole.Stop();

            Server?.Stop();
#if OPENNAT
            NatDiscoverer.ReleaseAll();
#endif
            Environment.Exit((int) ExitCodes.UnknownError);
        }



        public static long MainThreadTime { get; private set; }
        private static void Update()
        {
            var watch = Stopwatch.StartNew();
            while (true)
            {
                if (FastConsole.InputAvailable)
                {
                    var input = FastConsole.ReadLine();

                    if (input.StartsWith("/") && !ExecuteCommand(input))
                        FastConsole.WriteLine("Invalid command!");
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

            var exceptionText = CatchError(exception);
            ReportErrorLocal(exceptionText);
            ReportErrorWeb(exceptionText);
            Stop();
        }
        private static string CatchError(Exception ex)
        {
            var osInfo = OperatingSystemInfo.GetOperatingSystemInfo();

            // TODO: Log every physical cpu\gpu, not the first in entry
            var errorLog = 
$@"[CODE]
PokeD.Server.Desktop Crash Log v {Assembly.GetExecutingAssembly().GetName().Version}

Software:
    OS: {osInfo.Name} {osInfo.Architecture} [{(Type.GetType("Mono.Runtime") != null ? "Mono" : ".NET")}]
    Language: {CultureInfo.CurrentCulture.EnglishName}, LCID {osInfo.LocaleID}
    Framework: Version {osInfo.FrameworkVersion}
Hardware:
    CPU:
        Physical count: {osInfo.Hardware.CPUs.Count}
        Name: {osInfo.Hardware.CPUs.First().Name}
        Brand: {osInfo.Hardware.CPUs.First().Brand}
        Architecture: {osInfo.Hardware.CPUs.First().Architecture}
        Cores: {osInfo.Hardware.CPUs.First().Cores}
    GPU:
        Physical count: {osInfo.Hardware.GPUs.Count}
        Name: {osInfo.Hardware.GPUs.First().Name}
        Brand: {osInfo.Hardware.GPUs.First().Brand}
        Architecture: {osInfo.Hardware.GPUs.First().Architecture}
        Resolution: {osInfo.Hardware.GPUs.First().Resolution} {osInfo.Hardware.GPUs.First().RefreshRate} Hz
        Memory Total: {osInfo.Hardware.GPUs.First().MemoryTotal} KB
    RAM:
        Memory Total: {osInfo.Hardware.RAM.Total} KB
        Memory Free: {osInfo.Hardware.RAM.Free} KB

{BuildErrorStringRecursive(ex)}

You should report this error if it is reproduceable or you could not solve it by yourself.
Go To: {REPORTURL} to report this crash there.
[/CODE]";

            return errorLog;
        }
        private static string BuildErrorStringRecursive(Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendFormat(
$@"Error information:
Type: {ex.GetType().FullName}
Message: {ex.Message}
HelpLink: {(string.IsNullOrWhiteSpace(ex.HelpLink) ? "Empty" : ex.HelpLink)}
Source: {ex.Source}
TargetSite : {ex.TargetSite}
CallStack:
{ex.StackTrace}");

            if (ex.InnerException != null)
            {
                sb.AppendFormat($@"
--------------------------------------------------
InnerException:
{BuildErrorStringRecursive(ex.InnerException)}");
            }

            return sb.ToString();
        }
    }
}
