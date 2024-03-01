using Microsoft.Extensions.Logging;
using MqttServices.Core.Client;
using MqttServices.Core.Common;
using System.Text;
using System.Text.Json;

namespace MqttServices.Core.Services;

public class MessagingManager : IMessagingManager
{
    private readonly ILogger<MessagingManager> logger;
    private readonly IMqttClientService mqttClientService;
    private const string subscribeRequestTopic = "subscribeRequest";
    private const string resonseTopicSuffix = "__respnse";

    private Dictionary<string, dynamic> subscriptions = new Dictionary<string, dynamic>();

    public event EventHandler<Payload> RequestReceived;
    public event EventHandler<Payload> ResponseReceived;

    public MessagingManager(ILogger<MessagingManager> logger, IMqttClientService mqttClientService)
    {
        this.logger = logger;

        if (mqttClientService is not null)
        {
            this.mqttClientService = mqttClientService;
            this.mqttClientService.ClientConnected += MqttClientService_ClientConnected;
            this.mqttClientService.MessageReceived += MqttClientService_MessageReceived;
        }
    }

    private async void MqttClientService_MessageReceived(object? sender, MQTTnet.Client.MqttApplicationMessageReceivedEventArgs e)
    {
        if (e.ApplicationMessage.Topic == subscribeRequestTopic)
        {
            Payload payload = DeserializePayloadObject(e.ApplicationMessage.PayloadSegment);

            if (payload is not null)
            {
                RequestReceived?.Invoke(this, payload);
            }
        }
        if (e.ApplicationMessage.Topic.Contains(resonseTopicSuffix))
        {
            Payload payload = DeserializePayloadObject(e.ApplicationMessage.PayloadSegment);

            if (payload is not null)
            {
                ResponseReceived?.Invoke(this, payload);
            }
        }
    }
    public Payload DeserializePayloadObject(ArraySegment<byte> bytes)
    {
        try
        {
            var jsonString = Encoding.UTF8.GetString(bytes);
            Payload payload = JsonSerializer.Deserialize<Payload>(jsonString);

            var options = new JsonSerializerOptions();
            options.PropertyNameCaseInsensitive = true;

            Type genericType = Type.GetType(payload.ValueType);

            if (genericType != null)
            {
                var deserializeMethod = typeof(JsonSerializer)
                    .GetMethod(nameof(JsonSerializer.Deserialize), new[] { typeof(string), typeof(Type), typeof(JsonSerializerOptions) });

                object obj = deserializeMethod.Invoke(null, new object[] { payload.Value.ToString(), genericType, options });
                if (obj is not null)
                {
                    Payload retPayload = new Payload(payload.ExchangeName, obj);
                    retPayload.ValueType = payload.ValueType;
                    return retPayload;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error DeserializePayloadObject");
        }
        return null;
    }
    private async void MqttClientService_ClientConnected(object? sender, MQTTnet.Client.MqttClientConnectedEventArgs e)
    {
        await mqttClientService.Subscribe(subscribeRequestTopic);
    }

    public async Task SendMessageRequest<T>(T payload, string exchangeName)
    {
        if (mqttClientService.IsConnected)
        {
            // subscribe for Response:
            string topic = GetResponseTopic(exchangeName);
            await mqttClientService.Subscribe(topic);
            await mqttClientService.PublishMessage(subscribeRequestTopic, new Payload(exchangeName, payload));
        }
        else
        {
            Thread.Sleep(500);
            await SendMessageRequest(payload, exchangeName);
        }
    }
    public async Task SendMessageResponse<T>(T payload, string exchangeName)
    {
        if (mqttClientService.IsConnected)
        {
            string topic = GetResponseTopic(exchangeName);
            await mqttClientService.PublishMessage(topic, new Payload(exchangeName, payload));
        }
        else
        {
            Thread.Sleep(500);
            await SendMessageRequest(payload, exchangeName);
        }
    }

    private string GetRequestTopic(string exchangeName)
    {
        return string.Concat(exchangeName, "_request");
    }
    private string GetResponseTopic(string exchangeName)
    {
        return string.Concat(exchangeName, resonseTopicSuffix);
    }
    private static bool IsValidJson(string strInput)
    {
        try
        {
            JsonDocument.Parse(strInput);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
