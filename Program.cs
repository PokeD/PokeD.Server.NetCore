﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

using Aragas.Core.Wrappers;

using NDesk.Options;

using PCLStorage;

using PokeD.Core.Extensions;

using PokeD.Server.Desktop.WrapperInstances;

namespace PokeD.Server.Desktop
{
    public static partial class Program
    {
        private const string URL = "http://pokemon3d.net/forum/threads/12901/";
        private static Server Server { get; set; }


        static Program()
        {
            AppDomainWrapper.Instance = new AppDomainWrapperInstance();
            FileSystemWrapper.Instance = new FileSystemWrapperInstance();
            InputWrapper.Instance = new InputWrapperInstance();

            LuaWrapper.Instance = new MoonLuaWrapperInstance();

			DatabaseWrapper.Instance = new SQLiteDatabase();

            NancyWrapper.Instance = new NancyWrapperInstance();

            TCPClientWrapper.Instance = new TCPClientFactoryInstance();
            TCPListenerWrapper.Instance = new TCPServerWrapperInstance();
            ThreadWrapper.Instance = new ThreadWrapperInstance();

            PacketExtensions.Init();
        }

        public static void Main(params string[] args)
        {
            try { AppDomain.CurrentDomain.UnhandledException += (sender, e) => CatchErrorObject(e.ExceptionObject); }
            catch (Exception exception)
            {
                // Maybe it will cause a recursive exception.
                Server?.Stop();
                ConsoleManager.Stop();

                CatchError(exception);
            }

            #region Args parsing
            var options = new OptionSet();
            try
            {
                options = new OptionSet()
                    .Add("c|console", "enables the console", s => ConsoleManager.Start())
                    .Add("fps=", "{FPS} of the console, integer", fps => ConsoleManager.ScreenFPS = int.Parse(fps))
                    .Add("db=", "used {DATABASE_WRAPPER}", ParseDatabase)
                    .Add("lua=", "used {LUA_WRAPPER}", ParseLua)
                    .Add("h|help", "show help", str => ShowHelp(options));

                options.Parse(args);
            }
            catch (Exception ex) when (ex is OptionException || ex is FormatException)
            {
                ConsoleManager.Stop();

                Console.Write("PokeD.Server.Desktop: ");
                Console.WriteLine(ex.Message);
                Console.WriteLine("Try `PokeD.Server.Desktop --help' for more information.");

                ShowHelp(options, true);

                Console.ReadLine();
                return;
            }
            #endregion Args parsing

            Start();
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
                ConsoleManager.WriteLine("Usage: PokeD.Server.Desktop [OPTIONS]");
                ConsoleManager.WriteLine();
                ConsoleManager.WriteLine("Options:");

                var opt = new StringWriter();
                options.WriteOptionDescriptions(opt);
                foreach (var line in opt.GetStringBuilder().ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None))
                    ConsoleManager.WriteLine(line);
            }
        }
        private static void ParseDatabase(string db)
        {
            switch (db.ToLowerInvariant())
            {
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
            }
        }
        private static void ParseLua(string lua)
        {
            switch (lua.ToLowerInvariant())
            {
                case "ms":
                case "moon":
                case "moonsharp":
                case "filedb":
                    LuaWrapper.Instance = new MoonLuaWrapperInstance();
                    break;

                case "nl":
                case "nlua":
                    LuaWrapper.Instance = new NLuaWrapperInstance();
                    break;
            }
        }

        private static void Start()
        {
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
            var errorLog = 
$@"[CODE]
PokeD.Server.Desktop Crash Log v {Assembly.GetExecutingAssembly().GetName().Version}

System specifications:
Operating system: {Environment.OSVersion} [{(Type.GetType("Mono.Runtime") != null ? "Mono" : ".NET")}]
Core architecture: {(Environment.Is64BitOperatingSystem ? "64 Bit" : "32 Bit")}
System language: {CultureInfo.CurrentCulture.EnglishName}
Logical processors: {Environment.ProcessorCount}

{BuildErrorStringRecursive(ex)}

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
