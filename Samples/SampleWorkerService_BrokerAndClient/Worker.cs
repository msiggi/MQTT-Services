using MQTTnet.Client;
using MqttServices.Core.Broker;
using MqttServices.Core.Client;
using System.Text;
using System.Text.Json;

namespace SampleWorkerService_BrokerAndClient
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IMqttBrokerService mqttBrokerService;
        private readonly IMqttClientService mqttClientService;
        private int counter = 0;

        public Worker(ILogger<Worker> logger, IMqttBrokerService mqttBrokerService, IMqttClientService mqttClientService)
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

            var json = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            TestPayload payload = JsonSerializer.Deserialize<TestPayload>(json);

            _logger.LogInformation($"Received Payload: {payload.Name} {payload.DateTime} {payload.Number}");

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                counter++;
                TestPayload testPayload = new TestPayload
                {
                    Name = "test",
                    DateTime = DateTime.Now,
                    Number = counter
                };
                _logger.LogInformation($"Sending testPayload");
                await mqttClientService.PublishMessage("test/develop", testPayload);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}