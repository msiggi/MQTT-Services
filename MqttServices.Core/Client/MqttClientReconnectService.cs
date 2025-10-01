using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MqttServices.Core.Client;

public class MqttClientReconnectService : BackgroundService
{
    private readonly IMqttClientService _client;
    private readonly ILogger<MqttClientReconnectService> _logger;

    public MqttClientReconnectService(IMqttClientService client, ILogger<MqttClientReconnectService> logger)
    {
        _client = client;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_client.IsConnected && !_client.IsConnecting)
                    await _client.Connect();
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