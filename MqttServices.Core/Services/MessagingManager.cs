using Microsoft.Extensions.Logging;
using MqttServices.Core.Client;
using MqttServices.Core.Common;
using System.Buffers;
using System.Text;
using System.Text.Json;

namespace MqttServices.Core.Services;

public class MessagingManager : IMessagingManager, IDisposable
{
    private readonly ILogger<MessagingManager> logger;
    private readonly string exchangeTopicPrefix;
    private readonly IMqttClientService mqttClientService;
    private bool disposed = false;

    private const string resonseTopicSuffix = "__Respnse";
    private Dictionary<string, dynamic> subscriptions = new Dictionary<string, dynamic>();

    public string SubscribeRequestTopic { get => string.Concat(exchangeTopicPrefix, "subscribe__Request"); }
    public string SubscribeMessageTopic { get => string.Concat(exchangeTopicPrefix, "subscribe__Message"); }
    public string SubscribeMessageTopicDefaultExchange { get => string.Concat(exchangeTopicPrefix, "subscribe__DefaultMessage"); }
    public string SubscribeRequestTopicDefaultExchange { get => string.Concat(exchangeTopicPrefix, "subscribe__DefaultRequest"); }

    public event EventHandler<Payload> RequestReceived;
    public event EventHandler<Payload> RequestForGetOneReceived;
    public event EventHandler<Payload> RequestForGetAllReceived;
    public event EventHandler<Payload> RequestForUpsertReceived;
    public event EventHandler<Payload> RequestForUpdateReceived;
    public event EventHandler<Payload> RequestForInsertReceived;
    public event EventHandler<Payload> RequestForDeleteReceived;

    public event EventHandler<Payload> ResponseReceived;
    public event EventHandler<Payload> MessageReceived;

    public MessagingManager(ILogger<MessagingManager> logger, IMqttClientService mqttClientService)
    {
        this.logger = logger;
        if (mqttClientService is not null)
        {
            this.exchangeTopicPrefix = string.Concat(mqttClientService.ApplicationKey, "_");
            this.mqttClientService = mqttClientService;
            this.mqttClientService.ClientConnected += MqttClientService_ClientConnected;
            this.mqttClientService.MessageReceived += MqttClientService_MessageReceived;
            this.mqttClientService.Connect();
        }
    }

    private void MqttClientService_MessageReceived(object? sender, MQTTnet.MqttApplicationMessageReceivedEventArgs e)
    {
        if (e.ApplicationMessage.Topic == SubscribeRequestTopic || e.ApplicationMessage.Topic == SubscribeMessageTopicDefaultExchange)
        {
            Payload payload = DeserializePayloadObject(e.ApplicationMessage.Payload.ToArray());

            if (payload is not null)
            {
                switch (payload.RequestType)
                {
                    case RequestType.Generic:
                        RequestReceived?.Invoke(this, payload);
                        break;
                    case RequestType.GetOne:
                        RequestForGetOneReceived?.Invoke(this, payload);
                        break;
                    case RequestType.GetAll:
                        RequestForGetAllReceived?.Invoke(this, payload);
                        break;
                    case RequestType.Upsert:
                        RequestForUpsertReceived?.Invoke(this, payload);
                        break;
                    case RequestType.Update:
                        RequestForUpdateReceived?.Invoke(this, payload);
                        break;
                    case RequestType.Insert:
                        RequestForInsertReceived?.Invoke(this, payload);
                        break;
                    case RequestType.Delete:
                        RequestForDeleteReceived?.Invoke(this, payload);
                        break;
                    default:
                        break;
                }
            }

            return;
        }
        //if (e.ApplicationMessage.Topic == SubscribeRequestTopicDefaultExchange)
        //{
        //    Payload payload = DeserializePayloadObject(e.ApplicationMessage.Payload.ToArray());
        //    if (payload is not null)
        //    {
        //        RequestReceived?.Invoke(this, payload);
        //    }
        //}

        if (e.ApplicationMessage.Topic.Contains(resonseTopicSuffix))
        {
            Payload payload = DeserializePayloadObject(e.ApplicationMessage.Payload.ToArray());

            if (payload is not null)
            {
                ResponseReceived?.Invoke(this, payload);
            }
            return;
        }
        if (e.ApplicationMessage.Topic == SubscribeMessageTopic)
        {
            Payload payloadMessageReceived = DeserializePayloadObject(e.ApplicationMessage.Payload.ToArray());

            if (payloadMessageReceived is not null)
            {
                MessageReceived?.Invoke(this, payloadMessageReceived);
            }
        }
        if (e.ApplicationMessage.Topic == SubscribeMessageTopicDefaultExchange)
        {
            Payload payloadMessageReceived = DeserializePayloadObject(e.ApplicationMessage.Payload.ToArray());
            if (payloadMessageReceived is not null)
            {
                MessageReceived?.Invoke(this, payloadMessageReceived);
            }
        }
    }
    public Payload DeserializePayloadObject(byte[] bytes)
    {
        try
        {
            var jsonString = Encoding.UTF8.GetString(bytes);
            Payload payload = JsonSerializer.Deserialize<Payload>(jsonString);

            if (payload.Value is not null)
            {
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
                        //retPayload.ValueType = payload.ValueType;
                        retPayload.MessageId = payload.MessageId;
                        retPayload.RequestType = payload.RequestType;
                        return retPayload;
                    }
                }
            }
            else
            {
                return payload;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error DeserializePayloadObject");
        }
        return null;
    }
    private async void MqttClientService_ClientConnected(object? sender, MQTTnet.MqttClientConnectedEventArgs e)
    {
        logger.LogInformation("MqttClientService MQTT-Client connected!");
        await mqttClientService.Subscribe(SubscribeRequestTopic);
        await mqttClientService.Subscribe(SubscribeMessageTopic);
        await mqttClientService.Subscribe(SubscribeMessageTopicDefaultExchange);
        await mqttClientService.Subscribe(SubscribeRequestTopicDefaultExchange);
    }

    public async Task<Guid> SendMessageRequest<T>(T payload, string exchangeName)
    {
        Guid guid = Guid.NewGuid();
        if (mqttClientService.IsConnected)
        {
            // subscribe for Response:
            string topic = GetResponseTopic(exchangeName);
            await mqttClientService.Subscribe(topic);
            await mqttClientService.PublishMessage(SubscribeRequestTopic, new Payload(exchangeName, payload, guid));
        }
        else
        {
            Thread.Sleep(500);
            await SendMessageRequest(payload, exchangeName);
        }
        return guid;
    }
    public async Task<Guid> SendMessageRequest<T>(T payload)
    {
        Guid guid = Guid.NewGuid();
        if (mqttClientService.IsConnected && payload is not null)
        {
            // subscribe for Response:
            string exchangeName = payload.GetType().Name;
            string topic = GetResponseTopic(exchangeName);
            await mqttClientService.Subscribe(topic);
            await mqttClientService.PublishMessage(SubscribeRequestTopic, new Payload(exchangeName, payload, guid));
        }
        else
        {
            Thread.Sleep(500);
            await SendMessageRequest(payload);
        }
        return guid;
    }
    public async Task<Guid> SendMessageRequest<T>(T payload, RequestType requestType)
    {
        Guid guid = Guid.NewGuid();
        if (mqttClientService.IsConnected && payload is not null)
        {
            // subscribe for Response:
            string exchangeName = payload.GetType().Name;
            string topic = GetResponseTopic(exchangeName);
            await mqttClientService.Subscribe(topic);
            await mqttClientService.PublishMessage(SubscribeRequestTopic, new Payload(exchangeName, payload, guid, requestType));
        }
        else
        {
            Thread.Sleep(500);
            await SendMessageRequest(payload, requestType);
        }
        return guid;
    }
    public async Task<Guid> SendMessageRequest(string exchangeName, RequestType requestType)
    {
        Guid guid = Guid.NewGuid();
        if (mqttClientService.IsConnected)
        {
            // subscribe for Response:
            string topic = GetResponseTopic(exchangeName);
            await mqttClientService.Subscribe(topic);
            await mqttClientService.PublishMessage(SubscribeRequestTopic, new Payload(exchangeName, null, guid, requestType));
        }
        else
        {
            Thread.Sleep(500);
            await SendMessageRequest(exchangeName, requestType);
        }
        return guid;
    }

    /// <summary>
    /// To trigger something without response
    /// </summary>
    /// <param name="exchangeName"></param>
    /// <returns></returns>
    public async Task<Guid> SendMessageRequest(string exchangeName)
    {
        Guid guid = Guid.NewGuid();
        if (mqttClientService.IsConnected)
        {
            // subscribe for Response:
            string topic = GetResponseTopic(exchangeName);
            await mqttClientService.Subscribe(topic);
            await mqttClientService.PublishMessage(SubscribeRequestTopic, new Payload(exchangeName, null, guid));
        }
        else
        {
            Thread.Sleep(500);
            await SendMessageRequest(exchangeName);
        }
        return guid;
    }
    public async Task SendMessageResponse<T>(T payload, string exchangeName)
    {
        if (mqttClientService.IsConnected)
        {
            string topic = GetResponseTopic(exchangeName);

            if (payload is Payload)
            {
                await mqttClientService.PublishMessage(topic, payload as Payload);
            }
            else
            {
                await mqttClientService.PublishMessage(topic, new Payload(exchangeName, payload));
            }
        }
        else
        {
            Thread.Sleep(500);
            await SendMessageResponse(payload, exchangeName);
        }
    }
    public async Task SendMessageResponse(Payload payload)
    {
        if (mqttClientService.IsConnected && payload is not null)
        {
            string topic = GetResponseTopic(payload.ExchangeName);
            await mqttClientService.PublishMessage(topic, payload);
        }
        else
        {
            Thread.Sleep(500);
            await SendMessageResponse(payload);
        }
    }
    public async Task SendMessage<T>(T payload, string exchangeName)
    {
        if (mqttClientService.IsConnected)
        {
            await mqttClientService.PublishMessage(SubscribeMessageTopic, new Payload(exchangeName, payload));
        }
        else
        {
            Thread.Sleep(500);
            await SendMessage(payload, exchangeName);
        }
    }
    public async Task SendMessage<T>(T payload)
    {
        if (mqttClientService.IsConnected && payload is not null)
        {
            string exchangeName = payload.GetType().Name;
            await mqttClientService.PublishMessage(SubscribeMessageTopicDefaultExchange, new Payload(exchangeName, payload));
        }
        else
        {
            Thread.Sleep(500);
            await SendMessage(payload);
        }
    }

    private string GetResponseTopic(string exchangeName)
    {
        return string.Concat(exchangeTopicPrefix, exchangeName, resonseTopicSuffix);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                mqttClientService?.Disconnect();
                mqttClientService?.Dispose();
                this.mqttClientService.ClientConnected -= MqttClientService_ClientConnected;
                this.mqttClientService.MessageReceived -= MqttClientService_MessageReceived;
            }
            disposed = true;
        }
    }

}
