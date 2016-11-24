using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

using SystemInfoLibrary.Hardware.CPU;
using SystemInfoLibrary.Hardware.GPU;
using SystemInfoLibrary.OperatingSystem;

using PCLExt.FileStorage;

namespace PokeD.Server.Desktop
{
    public static partial class Program
    {
        private const string REPORTURL = "http://poked.github.io/report/";


        private static void CatchException(object exceptionObject)
        {
            var exception = exceptionObject as Exception ?? new NotSupportedException("Unhandled exception doesn't derive from System.Exception: " + exceptionObject);

            var exceptionText = CatchError(exception);
            ReportErrorLocal(exceptionText);
            ReportErrorWeb(exceptionText);
            Stop(true);
        }
        private static void CatchException(Exception exception)
        {
            var exceptionText = CatchError(exception);
            ReportErrorLocal(exceptionText);
            ReportErrorWeb(exceptionText);
            Stop(true);
        }

        private static string CatchError(Exception ex)
        {
            var osInfo = OperatingSystemInfo.GetOperatingSystemInfo();

            var errorLog =
$@"[CODE]
PokeD.Server.Desktop Crash Log v {Assembly.GetExecutingAssembly().GetName().Version}

Software:
    OS: {osInfo.Name} {osInfo.Architecture} [{(Type.GetType("Mono.Runtime") != null ? "Mono" : ".NET")}]
    Language: {CultureInfo.CurrentCulture.EnglishName}, LCID {osInfo.LocaleID}
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

            return errorLog;
        }
        private static string RecursiveCPU(IList<CPUInfo> cpus, int index)
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
        private static string RecursiveGPU(IList<GPUInfo> gpus, int index)
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
            var crashFile = Storage.CrashLogFolder.CreateFileAsync($"{DateTime.Now:yyyy-MM-dd_HH.mm.ss}.log", CreationCollisionOption.OpenIfExists).Result;
            using (var stream = crashFile.OpenAsync(PCLExt.FileStorage.FileAccess.ReadAndWrite).Result)
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
