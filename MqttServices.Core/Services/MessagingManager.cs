using Microsoft.Extensions.Logging;
using MqttServices.Core.Client;
using MqttServices.Core.Common;
using System.Buffers;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace MqttServices.Core.Services;

public class MessagingManager : IMessagingManager, IDisposable
{
    private readonly ILogger<MessagingManager> logger;
    private readonly string exchangeTopicPrefix;
    private readonly IMqttClientService mqttClientService;
    private bool disposed = false;

    private static readonly TimeSpan DefaultConnectWaitTimeout = TimeSpan.FromSeconds(5);

    private const string resonseTopicSuffix = "__Respnse";
    private Dictionary<string, dynamic> subscriptions = new Dictionary<string, dynamic>();

    public string SubscribeRequestTopic { get => string.Concat(exchangeTopicPrefix, "subscribe__Request"); }
    public string SubscribeMessageTopic { get => string.Concat(exchangeTopicPrefix, "subscribe__Message"); }

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
            // keine Verbindung im CTOR starten
        }
    }

    private void MqttClientService_MessageReceived(object? sender, MQTTnet.MqttApplicationMessageReceivedEventArgs e)
    {
        if (e.ApplicationMessage.Topic == SubscribeRequestTopic)
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
                        retPayload.MessageId = payload.MessageId;
                        retPayload.RequestType = payload.RequestType;
                        return retPayload;
                    }
                }
                else
                {
                    logger.LogError($"Error DeserializePayloadObject: Type not found. Please consider using a known type!" +
                        $" {Environment.NewLine}" +
                        $"Type: {payload.ValueType}");
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
    }

    // NEU: liefert false statt Exception
    private async Task<bool> EnsureConnectedAsync(TimeSpan timeout)
    {
        if (mqttClientService.IsConnected)
            return true;

        var sw = Stopwatch.StartNew();
        while (!mqttClientService.IsConnected && sw.Elapsed < timeout)
        {
            await Task.Delay(250);
        }

        if (!mqttClientService.IsConnected)
        {
            logger.LogWarning("MQTT nicht verbunden (Timeout {Timeout}s).", (int)timeout.TotalSeconds);
            return false;
        }
        return true;
    }

    public async Task<Guid> SendMessageRequest<T>(T payload, string exchangeName)
    {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        if (!await EnsureConnectedAsync(DefaultConnectWaitTimeout)) return Guid.Empty;

        Guid guid = Guid.NewGuid();

        string topic = GetResponseTopic(exchangeName);
        await mqttClientService.Subscribe(topic);
        await mqttClientService.PublishMessage(SubscribeRequestTopic, new Payload(exchangeName, payload, guid));
        return guid;
    }

    public async Task<Guid> SendMessageRequest<T>(T payload)
    {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        if (!await EnsureConnectedAsync(DefaultConnectWaitTimeout)) return Guid.Empty;

        Guid guid = Guid.NewGuid();
        string exchangeName = payload.GetType().Name;
        string topic = GetResponseTopic(exchangeName);
        await mqttClientService.Subscribe(topic);
        await mqttClientService.PublishMessage(SubscribeRequestTopic, new Payload(exchangeName, payload, guid));
        return guid;
    }

    public async Task<Guid> SendMessageRequest<T>(T payload, RequestType requestType)
    {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        if (!await EnsureConnectedAsync(DefaultConnectWaitTimeout)) return Guid.Empty;

        Guid guid = Guid.NewGuid();
        string exchangeName = payload.GetType().Name;
        string topic = GetResponseTopic(exchangeName);
        await mqttClientService.Subscribe(topic);
        await mqttClientService.PublishMessage(SubscribeRequestTopic, new Payload(exchangeName, payload, guid, requestType));
        return guid;
    }

    public async Task<Guid> SendMessageRequest(string exchangeName, RequestType requestType)
    {
        if (string.IsNullOrWhiteSpace(exchangeName)) throw new ArgumentNullException(nameof(exchangeName));
        if (!await EnsureConnectedAsync(DefaultConnectWaitTimeout)) return Guid.Empty;

        Guid guid = Guid.NewGuid();
        string topic = GetResponseTopic(exchangeName);
        await mqttClientService.Subscribe(topic);
        await mqttClientService.PublishMessage(SubscribeRequestTopic, new Payload(exchangeName, null, guid, requestType));
        return guid;
    }

    public async Task<Guid> SendMessageRequest(string exchangeName)
    {
        if (string.IsNullOrWhiteSpace(exchangeName)) throw new ArgumentNullException(nameof(exchangeName));
        if (!await EnsureConnectedAsync(DefaultConnectWaitTimeout)) return Guid.Empty;

        Guid guid = Guid.NewGuid();
        string topic = GetResponseTopic(exchangeName);
        await mqttClientService.Subscribe(topic);
        await mqttClientService.PublishMessage(SubscribeRequestTopic, new Payload(exchangeName, null, guid));
        return guid;
    }

    public async Task SendMessageResponse<T>(T payload, string exchangeName)
    {
        if (string.IsNullOrWhiteSpace(exchangeName)) throw new ArgumentNullException(nameof(exchangeName));
        if (!await EnsureConnectedAsync(DefaultConnectWaitTimeout)) return;

        string topic = GetResponseTopic(exchangeName);

        if (payload is Payload p)
        {
            await mqttClientService.PublishMessage(topic, p);
        }
        else
        {
            await mqttClientService.PublishMessage(topic, new Payload(exchangeName, payload));
        }
    }

    public async Task SendMessageResponse(Payload payload)
    {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        if (!await EnsureConnectedAsync(DefaultConnectWaitTimeout)) return;

        string topic = GetResponseTopic(payload.ExchangeName);
        await mqttClientService.PublishMessage(topic, payload);
    }

    public async Task SendMessage<T>(T payload, string exchangeName)
    {
        if (string.IsNullOrWhiteSpace(exchangeName)) throw new ArgumentNullException(nameof(exchangeName));
        if (!await EnsureConnectedAsync(DefaultConnectWaitTimeout)) return;

        await mqttClientService.PublishMessage(SubscribeMessageTopic, new Payload(exchangeName, payload));
    }

    public async Task SendMessage<T>(T payload)
    {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        if (!await EnsureConnectedAsync(DefaultConnectWaitTimeout)) return;

        string exchangeName = payload.GetType().Name;
        await mqttClientService.PublishMessage(SubscribeMessageTopic, new Payload(exchangeName, payload));
    }

    public async Task PublishMqttMessage(string topic, object payload)
    {
        if (string.IsNullOrWhiteSpace(topic)) throw new ArgumentNullException(nameof(topic));
        if (!await EnsureConnectedAsync(DefaultConnectWaitTimeout)) return;

        await mqttClientService.PublishMessage(topic, payload);
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
