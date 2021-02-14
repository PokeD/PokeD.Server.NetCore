using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Open.Nat;

using PokeD.Core;
using PokeD.Server.NetCore.Extensions;
using PokeD.Server.Services;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace PokeD.Server.NetCore
{
    public sealed class ServerManagerService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly ServerManagerOptions _options;

        private Task? _updateTask;
        private readonly CancellationTokenSource _stoppingCts = new();

        private IServiceProvider _serviceProvider;
        private CommandManagerService _commandManager;
        private ModuleManagerService _moduleManager;

        public ServerManagerService(ILogger<ServerManagerService> logger, IOptions<ServerManagerOptions> options, IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            //_commandManager = commandManager ?? throw new ArgumentNullException(nameof(commandManager));
            //_moduleManager = moduleManager ?? throw new ArgumentNullException(nameof(moduleManager));
        }

        private async Task UpdateAsync(CancellationToken ct)
        {
            var watch = Stopwatch.StartNew();
            while (!_stoppingCts.IsCancellationRequested)
            {
                string? input;
                if (!string.IsNullOrEmpty((input = Console.ReadLine())))
                {
                    if (input.StartsWith("/") && !await ExecuteCommandAsync(input))
                        _logger.Log(LogLevel.Information, new EventId(40, "Command"), "Invalid command!");
                }

                if (watch.ElapsedMilliseconds < 10)
                {
                    var time = (int)(10 - watch.ElapsedMilliseconds);
                    if (time < 0) time = 0;
                    await Task.Delay(time, ct);
                }

                watch.Reset();
                watch.Start();
            }

            _logger.LogWarning("Update loop stopped!");
        }

        private async Task<bool> ExecuteCommandAsync(string message)
        {
            var command = message.Remove(0, 1).ToLower();
            message = message.Remove(0, 1);

            if (command.StartsWith("stop"))
            {
                Program.LastRunTime = DateTime.UtcNow;
                await StopAsync(CancellationToken.None);
                NatDiscoverer.ReleaseAll();

                Console.WriteLine();
                Console.WriteLine("Stopped the server. Press any key to continue...");
                Console.ReadKey();
            }

            else if (command.StartsWith("clear"))
            {
                Console.Clear();
            }

            else
                return _commandManager.ExecuteServerCommand(message) == true;

            return true;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _commandManager = _serviceProvider.GetRequiredService<CommandManagerService>();
            _moduleManager = _serviceProvider.GetRequiredService<ModuleManagerService>();

            await NATForwardingAsync();

            _updateTask = UpdateAsync(_stoppingCts.Token);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Signal cancellation to the executing method
                _stoppingCts.Cancel();
            }
            finally
            {
                // Wait until the task completes or the stop token triggers
                await Task.WhenAny(_updateTask, Task.Delay(Timeout.Infinite, cancellationToken));
            }

            if (_options.NATForwardingEnabled)
                NatDiscoverer.ReleaseAll();
        }

        private async Task NATForwardingAsync()
        {
            if (!_options.NATForwardingEnabled)
                return;

            try
            {
                _logger.LogInformation("Initializing NAT Discovery.");
                var discoverer = new NatDiscoverer();
                _logger.LogInformation("Getting your external IP. Please wait...");
                var device = await discoverer.DiscoverDeviceAsync();
                _logger.LogInformation($"Your external IP is {device.GetExternalIPAsync().Wait(new CancellationTokenSource(2000))}.");

                foreach (var module in _moduleManager.GetModuleSettings().Where(module => module.Enabled && module.Port != 0))
                {
                    _logger.LogInformation($"Forwarding port {module.Port}.");
                    device.CreatePortMapAsync(new Mapping(Protocol.Tcp, module.Port, module.Port, "PokeD Port Mapping")).Wait(new CancellationTokenSource(2000).Token);
                }
            }
            catch (NatDeviceNotFoundException)
            {
                _logger.LogError("No NAT device is present or, Upnp is disabled in the router or Antivirus software is filtering SSDP (discovery protocol).");
            }
        }
    }
}