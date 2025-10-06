using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MqttServices.Core.Broker;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MqttServices.Core.Broker;

public class MqttBrokerHostedService : BackgroundService
{
    private readonly IMqttBrokerService _brokerService;
    private readonly ILogger<MqttBrokerHostedService> _logger;
    private readonly MqttBrokerSettings _settings;

    public MqttBrokerHostedService(IMqttBrokerService brokerService, IOptions<MqttBrokerSettings> settings, ILogger<MqttBrokerHostedService> logger)
    {
        _brokerService = brokerService;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.EnableBroker)
        {
            _logger.LogInformation("MqttBrokerHostedService: Broker disabled by config");
            return;
        }

        if (_settings.OpenFirewall)
        {
            TryOpenFirewall();
        }

        _logger.LogInformation("MqttBrokerHostedService starting broker...");
        try
        {
            await _brokerService.StartBroker();
            _logger.LogInformation("MqttBrokerHostedService started.");
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Ignored
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start MQTT broker");
        }
    }

    private void TryOpenFirewall()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                OpenWindowsFirewallRule(_settings.Port, encrypted: false);
                OpenWindowsFirewallRule(_settings.TlsPort, encrypted: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not configure firewall for MQTT Broker.");
        }
    }

    private void OpenWindowsFirewallRule(int? port, bool encrypted)
    {
        if (port is null or <= 0) return;
        try
        {
            string ruleName = encrypted ? "WinCCConnect MQTT Broker TLS" : "WinCCConnect MQTT Broker";
            string arguments = $"advfirewall firewall add rule name=\"{ruleName}\" dir=in action=allow protocol=TCP localport={port}";

            var psi = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = arguments,
                Verb = "runas",
                CreateNoWindow = true,
                UseShellExecute = true
            };
            Process.Start(psi);
            _logger.LogInformation("Firewall rule for MQTT Broker port {Port} added (if not already present).", port);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not add firewall rule for MQTT Broker port {Port}.", port);
        }
    }
}
