using MQTTnet;
using MqttServices.Core.Client;
using SampleCommon;
using System.Text;
using System.Text.Json;

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
            this.mqttClientService.ClientConnected += MqttClientService_ClientConnected;
            this.mqttClientService.MessageReceived += MqttClientService_MessageReceived;

            this.mqttClientService.Connect().Wait();
        }

        private void MqttClientService_MessageReceived(object? sender, MqttApplicationMessageReceivedEventArgs e)
        {
            if (e.ApplicationMessage.Topic == "testtopic")
            {
                var json = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                var recveivedObject = JsonSerializer.Deserialize<List<GuitarPlayer>>(json);

                recveivedObject.DumpToConsole();
            }
        }

        private void MqttClientService_ClientConnected(object? sender, MqttClientConnectedEventArgs e)
        {
            mqttClientService.Subscribe("testtopic");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                var guitarist = new GuitarPlayer
                {
                    Name = "Jimi Hendrix",
                    BirthDate = new DateTime(1942, 11, 27),
                    OwnedGuitars = new List<Guitar> { new Guitar { Model = "Stratocaster", Brand = "Fender", Color = "Sunburst" } }
                };

                var guitarists = new List<GuitarPlayer>
                {
                    new GuitarPlayer
                    {
                        Name = "Jimi Hendrix",
                        BirthDate = new DateTime(1942, 11, 27),
                        OwnedGuitars = new List<Guitar> { new Guitar { Model = "Stratocaster", Brand = "Fender", Color = "Sunburst" } }
                    },
                    new GuitarPlayer
                    {
                        Name = "Eric Clapton",
                        BirthDate = new DateTime(1945, 3, 30),
                        OwnedGuitars = new List<Guitar> { new Guitar { Model = "Stratocaster", Brand = "Fender", Color = "Black" } }
                    },
                    new GuitarPlayer                     {
                        Name = "Jimmy Page",
                        BirthDate = new DateTime(1944, 1, 9),
                        OwnedGuitars = new List<Guitar> { new Guitar { Model = "Les Paul", Brand = "Gibson", Color = "Sunburst" } }
                    }
                };

                await mqttClientService.PublishMessage("testtopic", guitarists);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}