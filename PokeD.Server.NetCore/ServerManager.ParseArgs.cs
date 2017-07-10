using System;
using System.Collections.Generic;

using ConsoleManager;

using PCLExt.Config;

using PokeD.Server.Modules;
using PokeD.Server.Services;

namespace PokeD.Server.NetCore
{
    public partial class ServerManager
    {
        private ConfigType ConfigType { get; set; } = ConfigType.JsonConfig;

        private bool NATForwardingEnabled { get; set; }

        private bool DisableUpdate { get; set; }

#if !NETCOREAPP2_0
        private void ParseArgs(IEnumerable<string> args)
        {
            var options = new NDesk.Options.OptionSet();
            try
            {
                options = new NDesk.Options.OptionSet()
                    .Add("c|console", "enables the console.", StartFastConsole)
                    .Add("fps=", "{FPS} of the console, integer.", fps => FastConsole.ScreenFPS = int.Parse(fps))
                    .Add("cf|config=", "used {CONFIG_WRAPPER}.", ParseConfig)
                    .Add("n|nat", "enables NAT port forwarding.", str => NATForwardingEnabled = true)
                    .Add("h|help", "show help.", str => ShowHelp(options))
                    .Add("l", "started via Launcher.", str => DisableUpdate = true);

                options.Parse(args);
            }
            catch (Exception ex) when (ex is NDesk.Options.OptionException || ex is FormatException)
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
        private void ShowHelp(NDesk.Options.OptionSet options, bool direct = false)
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
                Console.WriteLine("Usage: PokeD.Server.Desktop [OPTIONS]");
                Console.WriteLine();
                Console.WriteLine("Options:");

                var opt = new System.IO.StringWriter();
                options.WriteOptionDescriptions(opt);
                foreach (var line in opt.GetStringBuilder().ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None))
                    Console.WriteLine(line);
            }
        }
#else
        private void ParseArgs(IEnumerable<string> args)
        {
            StartFastConsole(string.Empty);
            NATForwardingEnabled = true;
            ParseConfig("yaml");
        }
#endif
        private void StartFastConsole(string s)
        {
            FastConsole.TitleFormatted = "PokeD Server FPS: {0}";
            //FastConsole.ConstantAddLine(
            //    "Main              thread execution time: {0} ms", () => new object[] { MainThreadTime });
            FastConsole.ConstantAddLine(
                "ModuleManagerUpdate thread execution time: {0} ms", () => new object[] { ModuleManagerService.UpdateThread });
            FastConsole.ConstantAddLine(
                "PlayerWatcher       thread execution time: {0} ms", () => new object[] { ModuleP3D.PlayerWatcherThreadTime });
            FastConsole.ConstantAddLine(
                "PlayerCorrection    thread execution time: {0} ms", () => new object[] { ModuleP3D.PlayerCorrectionThreadTime });
            FastConsole.ConstantAddLine(
                "ConsoleManager      thread execution time: {0} ms", () => new object[] { FastConsole.ConsoleManagerThreadTime });

            FastConsole.Start();
        }
        private void ParseConfig(string config)
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
                    throw new FormatException("Invalid CONFIG_WRAPPER.");
            }
        }
    }
}