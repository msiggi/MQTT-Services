﻿using Microsoft.Extensions.Logging;
using MqttServices.Core.Client;
using MqttServices.Core.Common;
using System.Text;
using System.Text.Json;

namespace MqttServices.Core.Services;

public class MessagingManager : IMessagingManager
{
    private readonly ILogger<MessagingManager> logger;
    private readonly IMqttClientService mqttClientService;
    private const string subscribeRequestTopic = "subscribe__Request";
    private const string subscribeMessageTopic = "subscribe__Message";
    private const string resonseTopicSuffix = "__respnse";

    private Dictionary<string, dynamic> subscriptions = new Dictionary<string, dynamic>();

    public event EventHandler<Payload> RequestReceived;
    public event EventHandler<Payload> ResponseReceived;
    public event EventHandler<Payload> MessageReceived;

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

            return;
        }
        if (e.ApplicationMessage.Topic.Contains(resonseTopicSuffix))
        {
            Payload payload = DeserializePayloadObject(e.ApplicationMessage.PayloadSegment);

            if (payload is not null)
            {
                ResponseReceived?.Invoke(this, payload);
            }
            return;
        }

        if (e.ApplicationMessage.Topic == subscribeMessageTopic)
        {
            Payload payloadMessageReceived = DeserializePayloadObject(e.ApplicationMessage.PayloadSegment);

            if (payloadMessageReceived is not null)
            {
                MessageReceived?.Invoke(this, payloadMessageReceived);
            }
        }


    }
    public Payload DeserializePayloadObject(ArraySegment<byte> bytes)
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
                        retPayload.ValueType = payload.ValueType;
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
    private async void MqttClientService_ClientConnected(object? sender, MQTTnet.Client.MqttClientConnectedEventArgs e)
    {
        await mqttClientService.Subscribe(subscribeRequestTopic);
        await mqttClientService.Subscribe(subscribeMessageTopic);
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
    public async Task SendMessageRequest(string exchangeName)
    {
        if (mqttClientService.IsConnected)
        {
            // subscribe for Response:
            string topic = GetResponseTopic(exchangeName);
            await mqttClientService.Subscribe(topic);
            await mqttClientService.PublishMessage(subscribeRequestTopic, new Payload(exchangeName, null));
        }
        else
        {
            Thread.Sleep(500);
            await SendMessageRequest(exchangeName);
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

    public async Task SendMessage<T>(T payload, string exchangeName)
    {
        if (mqttClientService.IsConnected)
        {
            await mqttClientService.PublishMessage(subscribeMessageTopic, new Payload(exchangeName, payload));
        }
        else
        {
            Thread.Sleep(500);
            await SendMessage(payload, exchangeName);
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
