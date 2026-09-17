using System.Net.NetworkInformation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MqttServices.Core.Broker;

namespace MqttServices.Broker.Service;

/// <summary>
/// Stops the service when the broker is not listening.
/// <para>
/// The library logs a failed broker start and carries on; for a host application that has other
/// work to do, that is the friendly behaviour. This service has no other work, so a process that
/// is up without a listening broker is the worst of both worlds: monitoring sees a healthy
/// service, clients see nothing. Stopping hands the decision to the Windows service recovery
/// policy, which restarts it.
/// </para>
/// </summary>
public class BrokerWatchdog : BackgroundService
{
    private readonly MqttBrokerSettings _brokerSettings;
    private readonly WatchdogSettings _settings;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<BrokerWatchdog> _logger;

    public BrokerWatchdog(
        IOptions<MqttBrokerSettings> brokerSettings,
        IOptions<WatchdogSettings> settings,
        IHostApplicationLifetime lifetime,
        ILogger<BrokerWatchdog> logger)
    {
        _brokerSettings = brokerSettings.Value;
        _settings = settings.Value;
        _lifetime = lifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogWarning("The broker watchdog is disabled by configuration.");
            return;
        }

        var ports = ExpectedPorts();
        if (ports.Count == 0)
        {
            _logger.LogWarning("No broker port is configured, so the watchdog has nothing to check.");
            return;
        }

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(_settings.StartupGraceSeconds), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        var consecutiveFailures = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            var missing = MissingListeners(ports);

            if (missing is null)
            {
                // The check itself failed. That says nothing about the broker, so leave the
                // counter where it is rather than shutting down over a failed enumeration.
                _logger.LogWarning("The watchdog could not read the list of listening ports; skipping this round.");
            }
            else if (missing.Count == 0)
            {
                consecutiveFailures = 0;
            }
            else
            {
                consecutiveFailures++;
                _logger.LogError(
                    "The broker is not listening on {Ports} ({Failures} of {Limit} checks).",
                    string.Join(", ", missing),
                    consecutiveFailures,
                    _settings.FailuresBeforeStop);

                if (consecutiveFailures >= _settings.FailuresBeforeStop)
                {
                    _logger.LogCritical(
                        "Stopping the service because the broker is not listening on {Ports}. " +
                        "The Windows service recovery policy restarts it; the reason for the " +
                        "failed start is further up in this log.",
                        string.Join(", ", missing));

                    _lifetime.StopApplication();
                    return;
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_settings.IntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private List<int> ExpectedPorts()
    {
        var ports = new List<int> { _brokerSettings.TlsPort };

        if (_brokerSettings.Port is > 0 and <= 65535)
        {
            ports.Add(_brokerSettings.Port.Value);
        }

        return ports;
    }

    /// <summary>
    /// Returns the configured ports nobody is listening on, or <c>null</c> when the check itself
    /// failed. Reading the listener table rather than connecting keeps the broker's log free of
    /// the watchdog's own aborted handshakes.
    /// </summary>
    private List<int>? MissingListeners(List<int> ports)
    {
        try
        {
            var listening = IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpListeners()
                .Select(endpoint => endpoint.Port)
                .ToHashSet();

            return ports.Where(port => !listening.Contains(port)).ToList();
        }
        catch (NetworkInformationException ex)
        {
            _logger.LogWarning(ex, "Could not read the list of listening ports.");
            return null;
        }
        catch (PlatformNotSupportedException ex)
        {
            _logger.LogWarning(ex, "Reading the list of listening ports is not supported here.");
            return null;
        }
    }
}
