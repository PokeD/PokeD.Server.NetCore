using System;
using System.Collections.Generic;
using System.IO;

using ConsoleManager;

using NDesk.Options;

using PCLExt.Config;
using PCLExt.Database;

namespace PokeD.Server.Desktop
{
    public static partial class Program
    {
        private static ConfigType ConfigType { get; set; } = ConfigType.YamlConfig;
        private static DatabaseType DatabaseType { get; set; } = DatabaseType.SQLiteDatabase;

#if OPENNAT
        private static bool NATForwardingEnabled { get; set; }
#endif


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
                Console.WriteLine("Press any key to continue$(SolutionDir).");
                Console.ReadKey();
                Environment.Exit((int)ExitCodes.Success);
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
                    DatabaseType = DatabaseType.FileDBDatabase;
                    break;

                case "sql":
                case "sqldb":
                case "sqlite":
                case "sqlitedb":
                    DatabaseType = DatabaseType.SQLiteDatabase;
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
                    ConfigType = ConfigType.JsonConfig;
                    break;

                case "yml":
                case "yaml":
                    ConfigType = ConfigType.YamlConfig;
                    break;

                default:
                    throw new FormatException("CONFIG_WRAPPER not correct.");
            }
        }
    }
}
