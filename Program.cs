using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;

using ConsoleManager;

using PokeD.Core.Extensions;
using PokeD.Server.Desktop.Extensions;

namespace PokeD.Server.Desktop
{
    public static partial class Program
    {
        private static Server Server { get; set; }


        static Program()
        {          
            PacketExtensions.Init();

            ServicePointManager.UseNagleAlgorithm = false;
        }


        public static void Main(params string[] args)
        {
            try { AppDomain.CurrentDomain.UnhandledException += (sender, e) => CatchException(e.ExceptionObject); }
            catch (Exception exception) { CatchException(exception); }

            ParseArgs(args);
            
            Start();
        }

        private static void Start()
        {
            Server = new Server(ConfigType);
            Server.Start();

            NATForwarding();

            Update();
        }
        private static void NATForwarding()
        {
            if (!NATForwardingEnabled)
                return;

            try
            {
                Logger.Log(LogType.Info, $"Initializing NAT Discovery.");
                var discoverer = new Open.Nat.NatDiscoverer();
                Logger.Log(LogType.Info, $"Getting your external IP. Please wait$(SolutionDir).");
                var device = discoverer.DiscoverDeviceAsync().Wait(new CancellationTokenSource(10000));
                Logger.Log(LogType.Info, $"Your external IP is {device.GetExternalIPAsync().Wait(new CancellationTokenSource(2000))}.");

                foreach (var module in Server.Modules.Where(module => module.Enabled && module.Port != 0))
                {
                    Logger.Log(LogType.Info, $"Forwarding port {module.Port}.");
                    device.CreatePortMapAsync(new Open.Nat.Mapping(Open.Nat.Protocol.Tcp, module.Port, module.Port, "PokeD Port Mapping")).Wait(new CancellationTokenSource(2000).Token);
                }
            }
            catch (Open.Nat.NatDeviceNotFoundException)
            {
                Logger.Log(LogType.Error, $"No NAT device is present or, Upnp is disabled in the router or Antivirus software is filtering SSDP (discovery protocol).");
            }
        }

        private static void Stop(bool error = false)
        {
            FastConsole.Stop();

            Server?.Stop();

            Open.Nat.NatDiscoverer.ReleaseAll();

            Environment.Exit(error ? (int) ExitCodes.UnknownError : (int) ExitCodes.Success);
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
                        Console.WriteLine("Invalid command!");
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
    }
}
