using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using PokeD.Core.Extensions;
using PokeD.Server.Storage.Files;

using Serilog;
using Serilog.Events;

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using PokeD.Server.Services;

namespace PokeD.Server.NetCore
{
    public static class Program
    {
        internal static DateTime LastRunTime { get; set; }

        static Program()
        {
            PacketExtensions.Init();

            ServicePointManager.UseNagleAlgorithm = false;

            // -- If somehow the exception was not handled in Main block, report it and fix it.
            AppDomain.CurrentDomain.UnhandledException += CatchException;
        }

        public static async Task Main(params string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.OutputEncoding = Encoding.UTF8;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            try
            {
                //CheckServerForUpdate();

                Log.Information("Starting Worker");
                var host = CreateHostBuilder(args).Build();
                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
        public static IHostBuilder CreateHostBuilder(string[]? args) => Host
            .CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureWebHostDefaults(webhost => webhost.UseStartup<Startup>())
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<SecurityService>();
                services.AddSingleton<DatabaseService>();
                services.AddSingleton<WorldService>();
                services.AddSingleton<ChatChannelManagerService>();
                services.AddSingleton<CommandManagerService>();
                services.AddSingleton<ModuleManagerService>();

                services.AddHostedService<SecurityService>(sp => sp.GetRequiredService<SecurityService>());
                services.Configure<DatabaseServiceOptions>(context.Configuration.GetSection("Database"));
                services.AddHostedService<DatabaseService>(sp => sp.GetRequiredService<DatabaseService>());
                services.AddHostedService<WorldService>(sp => sp.GetRequiredService<WorldService>());
                services.AddHostedService<ChatChannelManagerService>(sp => sp.GetRequiredService<ChatChannelManagerService>());
                services.AddHostedService<CommandManagerService>(sp => sp.GetRequiredService<CommandManagerService>());
                services.AddHostedService<ModuleManagerService>(sp => sp.GetRequiredService<ModuleManagerService>());

                services.Configure<ServerOptions>(context.Configuration.GetSection("Options"));
                services.AddHostedService<ServerManagerService>();
            });


        private static readonly string REPORTURL = "http://poked.github.io/report/";

        private static void CatchException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception ?? new NotSupportedException("Unhandled exception doesn't derive from System.Exception: " + e.ExceptionObject);
            CatchError(exception);
        }
        private static void CatchException(Exception exception)
        {
            var exceptionText = CatchError(exception);
            ReportErrorLocal(exceptionText);
            ReportErrorWeb(exceptionText);
        }

        private static string CatchError(Exception ex)
        {
#if !NET5_0
            var osInfo = SystemInfoLibrary.OperatingSystem.OperatingSystemInfo.GetOperatingSystemInfo();
#else
            var platformService = Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default;
#endif

            var errorLog =
#if !NET5_0
                $@"[CODE]
PokeD.Server.Desktop Crash Log v {Assembly.GetExecutingAssembly().GetName().Version}

Software:
    OS: {osInfo.Name} {osInfo.Architecture} [{(Type.GetType("Mono.Runtime") != null ? "Mono" : ".NET")}]
    Language: {CultureInfo.CurrentCulture.EnglishName}
    Framework: Version {osInfo.FrameworkVersion}
Hardware:
{RecursiveCPU(osInfo.Hardware.CPUs, 0)}
{RecursiveGPU(osInfo.Hardware.GPUs, 0)}
    RAM:
        Memory Total: {osInfo.Hardware.RAM.Total} KB
        Memory Free: {osInfo.Hardware.RAM.Free} KB

{RecursiveException(ex)}

You should report this error if it is reproduceable or you could not solve it by yourself.
Go To: {REPORTURL} to report this crash there.
[/CODE]";
#else
                $@"[CODE]
PokeD.Server.Desktop Crash Log v {Assembly.GetExecutingAssembly().GetName().Version}

Software:
    OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription} {System.Runtime.InteropServices.RuntimeInformation.OSArchitecture} [{platformService.Application.RuntimeFramework.FullName}]
    Language: {CultureInfo.CurrentCulture.EnglishName}
    Framework:
        Runtime {typeof(System.Runtime.InteropServices.RuntimeInformation).GetTypeInfo().Assembly.GetCustomAttributes().OfType<AssemblyInformationalVersionAttribute>().Single().InformationalVersion}

{RecursiveException(ex)}

You should report this error if it is reproduceable or you could not solve it by yourself.
Go To: {REPORTURL} to report this crash there.
[/CODE]";
#endif

            return errorLog;
        }
#if !NET5_0
        private static string RecursiveCPU(System.Collections.Generic.IList<SystemInfoLibrary.Hardware.CPU.CPUInfo> cpus, int index)
        {
            var sb = new StringBuilder();
            sb.AppendFormat(
                $@"    CPU{index}:
        Name: {cpus[index].Name}
        Brand: {cpus[index].Brand}
        Architecture: {cpus[index].Architecture}
        Cores: {cpus[index].Cores}");

            if (index + 1 < cpus.Count)
                sb.AppendFormat(RecursiveCPU(cpus, ++index));

            return sb.ToString();
        }
        private static string RecursiveGPU(System.Collections.Generic.IList<SystemInfoLibrary.Hardware.GPU.GPUInfo> gpus, int index)
        {
            var sb = new StringBuilder();
            sb.AppendFormat(
                $@"    GPU{index}:
        Name: {gpus[index].Name}
        Brand: {gpus[index].Brand}
        Resolution: {gpus[index].Resolution} {gpus[index].RefreshRate} Hz
        Memory Total: {gpus[index].MemoryTotal} KB");

            if (index + 1 < gpus.Count)
                sb.AppendFormat(RecursiveGPU(gpus, ++index));

            return sb.ToString();
        }
#endif
        private static string RecursiveException(Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendFormat(
                $@"Exception information:
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
{RecursiveException(ex.InnerException)}");
            }

            return sb.ToString();
        }

        private static void ReportErrorLocal(string exception)
        {
            using (var stream = new CrashLogFile().Open(PCLExt.FileStorage.FileAccess.ReadAndWrite))
            using (var writer = new StreamWriter(stream))
                writer.Write(exception);
        }
        private static void ReportErrorWeb(string exception)
        {
            //if (!Server.AutomaticErrorReporting)
            //    return;
        }

        /*
        private static void ParseArgs(IEnumerable<string> args)
        {
            var options = new OptionSet();
            try
            {
                options = new OptionSet()
                    .Add("cf|config=", "used {CONFIG_WRAPPER} [json/yaml].", ParseConfig)
                    .Add("n|nat", "enables NAT port forwarding.", str => NATForwardingEnabled = true)
                    .Add("h|help", "show help.", str => ShowHelp(options))
                    .Add("l", "started via Launcher (disable update check).", str => DisableUpdate = true);

                options.Parse(args);
            }
            catch (Exception ex) when (ex is OptionException || ex is FormatException)
            {
                Console.Write("PokeD.Server.NetCore: ");
                Console.WriteLine(ex.Message);
                Console.WriteLine("Try `PokeD.Server.NetCore --help' for more information.");

                ShowHelp(options, true);

                Console.WriteLine();
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                Environment.Exit((int) ExitCodes.Success);
            }
        }
        */
    }
}