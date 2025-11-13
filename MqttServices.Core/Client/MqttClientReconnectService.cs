using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MqttServices.Core.Discovery;

namespace MqttServices.Core.Client;

public class MqttClientReconnectService : BackgroundService
{
    private readonly IMqttClientService _client;
    private readonly ILogger<MqttClientReconnectService> _logger;
    private readonly IDiscoveryClient _discoveryClient;
    private readonly MqttClientSettings _settings;

    // Retry discovery periodically until connected
    private DateTimeOffset _nextDiscoveryAttemptUtc = DateTimeOffset.MinValue;
    private static readonly TimeSpan DiscoveryRetryInterval = TimeSpan.FromSeconds(30);

    public MqttClientReconnectService(
        IMqttClientService client,
        ILogger<MqttClientReconnectService> logger,
        IDiscoveryClient discoveryClient,
        IOptions<MqttClientSettings> clientOptions)
    {
        _client = client;
        _logger = logger;
        _discoveryClient = discoveryClient;
        _settings = clientOptions.Value;

        // Diagnostic: log construction so we can see when DI constructs the instance
        _logger.LogInformation("MqttClientReconnectService constructed. Enabled={Enabled}", _settings?.Enabled);
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MqttClientReconnectService.StartAsync() called by host.");
        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MqttClientReconnectService.StopAsync() called by host.");
        await base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MqttClientReconnectService ExecuteAsync started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_client.IsConnected && !_client.IsConnecting)
                {
                    var discoveryEnabled = _settings.Discovery?.SearchForDiscoveryServer == true;
                    var now = DateTimeOffset.UtcNow;

                    if (discoveryEnabled && now >= _nextDiscoveryAttemptUtc)
                    {
                        _logger.LogInformation("Attempting MQTT discovery broadcast on UDP {Port} ...", _settings.Discovery!.Port);
                        try
                        {
                            await _discoveryClient.SendBroadcastDiscoveryRequest();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Discovery broadcast failed.");
                        }

                        // schedule next attempt
                        _nextDiscoveryAttemptUtc = now.Add(DiscoveryRetryInterval);

                        // If discovery triggered a reconnect/connect, skip direct connect in this iteration
                        if (_client.IsConnecting || _client.IsConnected)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                            continue;
                        }
                    }

                    await _client.Connect();
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MQTT reconnect attempt failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}