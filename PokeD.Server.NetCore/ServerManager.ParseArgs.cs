using System;
using System.Collections.Generic;

using CommandLine.Options;

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

        private void ParseArgs(IEnumerable<string> args)
        {
            var options = new OptionSet();
            try
            {
                options = new OptionSet()
                    .Add("c|console", "enables the console.", StartFastConsole)
                    .Add("fps=", "{FPS} of the console, integer.", fps => ConsoleEx.ScreenFPS = int.Parse(fps))
                    .Add("cf|config=", "used {CONFIG_WRAPPER}.", ParseConfig)
                    .Add("n|nat", "enables NAT port forwarding.", str => NATForwardingEnabled = true)
                    .Add("h|help", "show help.", str => ShowHelp(options))
                    .Add("l", "started via Launcher.", str => DisableUpdate = true);

                options.Parse(args);
            }
            catch (Exception ex) when (ex is OptionException || ex is FormatException)
            {
                ConsoleEx.Stop();

                Console.Write("PokeD.Server.Desktop: ");
                Console.WriteLine(ex.Message);
                Console.WriteLine("Try `PokeD.Server.NetCore --help' for more information.");

                ShowHelp(options, true);

                Console.WriteLine();
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                Environment.Exit((int) ExitCodes.Success);
            }
        }
        private void ShowHelp(OptionSet options, bool direct = false)
        {
            if (direct)
            {
                Console.WriteLine("Usage: PokeD.Server.NetCore [OPTIONS]");
                Console.WriteLine();
                Console.WriteLine("Options:");

                options.WriteOptionDescriptions(Console.Out);
            }
            else
            {
                Console.WriteLine("Usage: PokeD.Server.NetCore [OPTIONS]");
                Console.WriteLine();
                Console.WriteLine("Options:");

                var opt = new System.IO.StringWriter();
                options.WriteOptionDescriptions(opt);
                foreach (var line in opt.GetStringBuilder().ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None))
                    Console.WriteLine(line);
            }
        }
        /*
        private void ParseArgs(IEnumerable<string> args)
        {
            StartFastConsole(string.Empty);
            NATForwardingEnabled = false;
            ParseConfig("yaml");
        }
        */
        private void StartFastConsole(string s)
        {
            ConsoleEx.TitleFormatted = "PokeD Server FPS: {0}";
            ConsoleEx.ConstantAddLine(
                "ModuleManagerUpdate thread execution time: {0} ms", () => new object[] { ModuleManagerService.UpdateThread });
            ConsoleEx.ConstantAddLine(
                "PlayerWatcher       thread execution time: {0} ms", () => new object[] { ModuleP3D.PlayerWatcherThreadTime });
            ConsoleEx.ConstantAddLine(
                "PlayerCorrection    thread execution time: {0} ms", () => new object[] { ModuleP3D.PlayerCorrectionThreadTime });

            ConsoleEx.Start();
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