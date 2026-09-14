using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MqttServices.Core.Broker;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MqttServices.Core.Discovery;

public class DiscoveryServerHostedService : BackgroundService
{
    private readonly DiscoveryServer _server;
    private readonly ILogger<DiscoveryServerHostedService> _logger;
    private readonly MqttBrokerSettings _settings;

    public DiscoveryServerHostedService(DiscoveryServer server, IOptions<MqttBrokerSettings> options, ILogger<DiscoveryServerHostedService> logger)
    {
        _server = server;
        _logger = logger;
        _settings = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var discovery = _settings.Discovery;
        if (!discovery.Enabled)
        {
            _logger.LogInformation("DiscoveryServer disabled by configuration.");
            return;
        }

        if (discovery.OpenFirewall)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    try
                    {
                        string ruleName = "WinCCConnect Discovery Server";
                        string arguments = $"advfirewall firewall add rule name=\"{ruleName}\" dir=in action=allow protocol=UDP localport={discovery.Port}";

                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = "netsh",
                            Arguments = arguments,
                            Verb = "runas",
                            CreateNoWindow = true,
                            UseShellExecute = true
                        };
                        Process.Start(psi);
                        _logger.LogInformation("Firewall rule for Discovery Server port {Port} added (if not already present).", discovery.Port);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not add firewall rule for Discovery Server port.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not configure firewall for Discovery Server.");
            }
        }

        _logger.LogInformation("Starting DiscoveryServer on UDP {Port} ...", discovery.Port);
        var brokerPort = _settings.TlsPort; // or map to plain Port if desired
        await _server.StartServer(discovery.Port, brokerPort, stoppingToken);
    }
}
