using MQTTnet.Client;
using MqttServices.Core.Broker;
using MqttServices.Core.Client;
using System.Text.Json;
using System.Text;

namespace SampleWorkerService_BrokerAndClient
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly MqttBrokerService mqttBrokerService;
        private readonly MqttClientService mqttClientService;

        public Worker(ILogger<Worker> logger, MqttBrokerService mqttBrokerService, MqttClientService mqttClientService)
        {
            _logger = logger;
            this.mqttBrokerService = mqttBrokerService;
            this.mqttClientService = mqttClientService;
            this.mqttClientService.MessageReceived += MqttClientService_MessageReceived;
            this.mqttClientService.ClientConnected += MqttClientService_ClientConnected;
        }

        private void MqttClientService_ClientConnected(object? sender, MqttClientConnectedEventArgs e)
        {
            _logger.LogInformation($"MQTT-Client connected!");
            this.mqttClientService.Subscribe("test/develop");
        }

        private void MqttClientService_MessageReceived(object? sender, MqttApplicationMessageReceivedEventArgs e)
        {
            _logger.LogInformation($"Receiving {e.ApplicationMessage.Topic}");

            var json = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            string payload = JsonSerializer.Deserialize<string>(json);

            _logger.LogInformation($"Received Payload: {payload}");

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var testPayload = $"Test-Payload {DateTimeOffset.Now}";
                _logger.LogInformation($"Sending {testPayload}");
                await mqttClientService.PublishMessage("test/develop", testPayload);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}