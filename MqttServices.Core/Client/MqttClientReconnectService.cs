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
    private bool _discoveryAttempted;

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
        _discoveryAttempted = false;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_client.IsConnected && !_client.IsConnecting)
                {
                    if (_settings.Discovery?.SearchForDiscoveryServer == true && !_discoveryAttempted)
                    {
                        _discoveryAttempted = true; // ensure only once at startup
                        _logger.LogInformation("Attempting MQTT discovery broadcast on UDP {Port} ...", _settings.Discovery.Port);
                        try
                        {
                            await _discoveryClient.SendBroadcastDiscoveryRequest();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Discovery broadcast failed.");
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