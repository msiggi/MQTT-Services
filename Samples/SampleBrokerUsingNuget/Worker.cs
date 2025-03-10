using MqttServices.Core.Broker;

namespace SampleBrokerUsingNuget
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IMqttBrokerService mqttBrokerService;

        public Worker(ILogger<Worker> logger, IMqttBrokerService mqttBrokerService)
        {
            _logger = logger;
            this.mqttBrokerService = mqttBrokerService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
