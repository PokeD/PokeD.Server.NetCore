using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using SystemInfoLibrary.OperatingSystem;

using Aragas.Core.Wrappers;

using FireSharp;
using FireSharp.Config;

using PCLStorage;

namespace PokeD.Server.Desktop
{
    public static partial class Program
    {
        private class FBReport
        {
            public string Description;
            public string ErrorCode;
            public DateTime Date;

            public FBReport(string description, string errorCode, DateTime date)
            {
                Description = description;
                ErrorCode = errorCode;
                Date = date;
            }
        }


        private const string REPORTURL = "http://poked.github.io/report/";
        private const string FBURL = "https://poked.firebaseio.com/";


        private static void CatchException(object exceptionObject)
        {
            var exception = exceptionObject as Exception ?? new NotSupportedException("Unhandled exception doesn't derive from System.Exception: " + exceptionObject);

            var exceptionText = CatchError(exception);
            ReportErrorLocal(exceptionText);
            ReportErrorWeb(exceptionText);
            Stop();
        }
        private static void CatchException(Exception exception)
        {
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
            client.Push("", new FBReport("Sent from PokeD", exception, DateTime.Now));
        }
    }
}
