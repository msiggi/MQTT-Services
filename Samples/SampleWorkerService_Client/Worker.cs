using MqttServices.Core.Client;

namespace SampleWorkerService_Client
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IMqttClientService mqttClientService;

        public Worker(ILogger<Worker> logger, IMqttClientService mqttClientService)
        {
            _logger = logger;
            this.mqttClientService = mqttClientService;
        }



        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                mqttClientService.PublishMessage("testtopic", "this is a test-payload");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}