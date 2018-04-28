using System;
using System.Collections.Generic;

using CommandLine.Options;

using PCLExt.Config;

namespace PokeD.Server.NetCore
{
    public partial class ServerManager
    {
        private ConfigType ConfigType { get; set; } = ConfigType.YamlConfig;

        private bool NATForwardingEnabled { get; set; }

        private bool DisableUpdate { get; set; }

        private void ParseArgs(IEnumerable<string> args)
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
            Console.WriteLine("Usage: PokeD.Server.NetCore [OPTIONS]");
            Console.WriteLine();
            Console.WriteLine("Options:");

            if (direct)
            {
                options.WriteOptionDescriptions(Console.Out);
            }
            else
            {
                var opt = new System.IO.StringWriter();
                options.WriteOptionDescriptions(opt);
                foreach (var line in opt.GetStringBuilder().ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None))
                    Console.WriteLine(line);
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            Environment.Exit((int) ExitCodes.Success);
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