using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using ConsoleManager;

using Open.Nat;

using PokeD.Core.Data.PokeApi;
using PokeD.Server.NetCore.Extensions;
using PokeD.Server.Services;

namespace PokeD.Server.NetCore
{
    public partial class ServerManager : IDisposable
    {
        private Server Server { get; set; }
        private CancellationTokenSource UpdateToken { get; set; }

        public ServerManager()
        {
            PokeApiV2.CacheType = PokeApiV2.CacheTypeEnum.Zip;

            Logger.LogMessage += Logger_LogMessage;
        }

        private void Logger_LogMessage(object sender, LogEventArgs logEventArgs)
        {
            var str = string.Format(logEventArgs.DefaultFormat, logEventArgs.DateTime, logEventArgs.Message);
            LogManager.WriteLine(str);
            Console.WriteLine(str);
        }

        public void Run(string[] args)
        {
            ParseArgs(args);

            CheckServerForUpdate();

            Start();
        }


        private void Start()
        {
            Server = new Server(ConfigType);
            Server.Start();

            NATForwarding();

            UpdateToken = new CancellationTokenSource();
            Update();
        }
        private async void NATForwarding()
        {
            if (!NATForwardingEnabled)
                return;

            try
            {
                Logger.Log(LogType.Info, "Initializing NAT Discovery.");
                var discoverer = new NatDiscoverer();
                Logger.Log(LogType.Info, "Getting your external IP. Please wait...");
                var device = await discoverer.DiscoverDeviceAsync();
                Logger.Log(LogType.Info, $"Your external IP is {device.GetExternalIPAsync().Wait(new CancellationTokenSource(2000))}.");

                foreach (var module in Server.GetService<ModuleManagerService>().GetModuleSettings().Where(module => module.Enabled && module.Port != 0))
                {
                    Logger.Log(LogType.Info, $"Forwarding port {module.Port}.");
                    device.CreatePortMapAsync(new Mapping(Protocol.Tcp, module.Port, module.Port, "PokeD Port Mapping")).Wait(new CancellationTokenSource(2000).Token);
                }
            }
            catch (NatDeviceNotFoundException)
            {
                Logger.Log(LogType.Error, "No NAT device is present or, Upnp is disabled in the router or Antivirus software is filtering SSDP (discovery protocol).");
            }
        }

        private void Stop(bool error = false)
        {
            UpdateToken?.Cancel();
            Server?.Stop();

            if (NATForwardingEnabled)
                NatDiscoverer.ReleaseAll();

            ConsoleEx.Stop();
        }

        
        private void Update()
        {
            var watch = Stopwatch.StartNew();
            
            while (!UpdateToken.IsCancellationRequested)
            {
                string input;
                if (!string.IsNullOrEmpty((input = Console.ReadLine())))
                {
                    if (input.StartsWith("/") && !ExecuteCommand(input))
                        Logger.Log(LogType.Command, "Invalid command!");
                }

                if(UpdateToken.IsCancellationRequested || Server == null || (Server != null && Server.IsDisposing))
                    break;

                //Server.Update();


                if (watch.ElapsedMilliseconds < 10)
                {
                    var time = (int) (10 - watch.ElapsedMilliseconds);
                    if (time < 0) time = 0;
                    Thread.Sleep(time);
                }

                watch.Reset();
                watch.Start();
            }

            Logger.Log(LogType.Warning, "Update loop stopped!");
        }


        public void Dispose()
        {
            Logger.LogMessage -= Logger_LogMessage;

            Server?.Dispose();
            
            //UpdateToken.Dispose();
        }
    }
}