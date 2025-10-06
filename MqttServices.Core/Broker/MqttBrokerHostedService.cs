using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MqttServices.Core.Broker;

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
}
