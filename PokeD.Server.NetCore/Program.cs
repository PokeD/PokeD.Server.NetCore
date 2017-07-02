using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using PCLExt.FileStorage;
using PokeD.Core.Extensions;
using PokeD.Server.Storage.Files;

namespace PokeD.Server.NetCore
{
    public static class Program
    {
        private static DateTime LastRunTime { get; set; }

        static Program()
        {
            PacketExtensions.Init();

            ServicePointManager.UseNagleAlgorithm = false;

        }

        public static void Main(params string[] args)
        {
            ServerManager serverManager = null;
            Start:
            try
            {
                LastRunTime = DateTime.UtcNow;
                serverManager = new ServerManager();
                serverManager.Run(args);
            }
            catch (Exception e)
            {
                CatchException(e);

                if (DateTime.UtcNow - LastRunTime > new TimeSpan(0, 0, 0, 10))
                {
                    serverManager?.Dispose();
                    goto Start;
                }
                else
                    Environment.Exit((int) ExitCodes.UnknownError);
            }
            finally
            {
                serverManager?.Dispose();
            }

            Environment.Exit((int) ExitCodes.Success);
        }


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
#if !NETCOREAPP2_0
            var osInfo = SystemInfoLibrary.OperatingSystem.OperatingSystemInfo.GetOperatingSystemInfo();
#else
            var platformService = Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default;
#endif

            var errorLog =
#if !NETCOREAPP2_0
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

{RecursiveException(ex)}

You should report this error if it is reproduceable or you could not solve it by yourself.
Go To: {REPORTURL} to report this crash there.
[/CODE]";
#endif

            return errorLog;
        }
#if !NETCOREAPP2_0
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
    }
}