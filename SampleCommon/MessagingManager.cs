
using Microsoft.Extensions.Logging;
using MqttServices.Core.Client;
using System.Text;

namespace SampleCommon
{
    public class MessagingManager : IMessagingManager
    {
        private readonly ILogger<MessagingManager> logger;
        private readonly MqttClientService mqttClientService;
        private const string requestTopic = "myRequestTopic";
        public event EventHandler<Payload> RequestReceived;

        public MessagingManager(ILogger<MessagingManager> logger, MqttClientService mqttClientService)
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
        }

        private async void MqttClientService_ClientConnected(object? sender, MQTTnet.Client.MqttClientConnectedEventArgs e)
        {
            await mqttClientService.Subscribe(requestTopic);
        }

        public Task RequestMessage()
        {
            throw new NotImplementedException();
        }
    }
    public interface IMessagingManager
    {
        event EventHandler<Payload> RequestReceived;
        Task RequestMessage();

    }

}
