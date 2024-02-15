
using Microsoft.Extensions.Logging;
using MqttServices.Core.Client;
using System.Text;

namespace SampleCommon
{
    public class MessagingManager : IMessagingManager
    {
        private readonly ILogger<MessagingManager> logger;
        private readonly IMqttClientService mqttClientService;
        private const string baseTopic = "baseTopic";
        private string requestTopic = $"{baseTopic}_request";
        private string responseTopic = $"{baseTopic}_response";
        public event EventHandler<Payload> RequestReceived;
        public event EventHandler<Payload> ResponseReceived;

        public MessagingManager(ILogger<MessagingManager> logger, IMqttClientService mqttClientService)
        {
            this.logger = logger;
            this.mqttClientService = mqttClientService;
            this.mqttClientService.ClientConnected += MqttClientService_ClientConnected;
            this.mqttClientService.MessageReceived += MqttClientService_MessageReceived;
        }

        private void MqttClientService_MessageReceived(object? sender, MQTTnet.Client.MqttApplicationMessageReceivedEventArgs e)
        {
            if (e.ApplicationMessage.Topic == requestTopic)
            {
                Payload payload = null;
                try
                {
                    var json = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                    payload = System.Text.Json.JsonSerializer.Deserialize<Payload>(json);
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, $"Parsing MQTT-Message for {e.ApplicationMessage.Topic} not successful!");
                }
                if (payload != null)
                {
                    RequestReceived?.Invoke(this, payload);
                }
            }
            if (e.ApplicationMessage.Topic == responseTopic)
            {
                Payload payload = null;
                try
                {
                    var json = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                    payload = System.Text.Json.JsonSerializer.Deserialize<Payload>(json);
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, $"Parsing MQTT-Message for {e.ApplicationMessage.Topic} not successful!");
                }
                if (payload != null)
                {
                    ResponseReceived?.Invoke(this, payload);
                }
            }
        }

        private async void MqttClientService_ClientConnected(object? sender, MQTTnet.Client.MqttClientConnectedEventArgs e)
        {
            await mqttClientService.Subscribe(requestTopic);
            await mqttClientService.Subscribe(responseTopic);
        }

        public async Task SendMessageRequest(Payload payload)
        {
            if (mqttClientService.IsConnected)
            {
                await mqttClientService.PublishMessage(requestTopic, payload);
                logger?.LogInformation($"RequestMessage {requestTopic} was sent!");
            }
            else
            {
                Thread.Sleep(1000);
                await SendMessageRequest(payload);
            }
        }

        public async Task SendMessageResponse(Payload payload)
        {
            await mqttClientService.PublishMessage(responseTopic, payload);
        }
    }
    public interface IMessagingManager
    {
        event EventHandler<Payload> RequestReceived;
        event EventHandler<Payload> ResponseReceived;
        Task SendMessageRequest(Payload payload);
        Task SendMessageResponse(Payload payload);

    }

}
