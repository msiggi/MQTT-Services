
using Microsoft.Extensions.Logging;
using MqttServices.Core.Client;
using System.Text;
using System.Text.Json;

namespace SampleCommon
{
    public class MessagingManager : IMessagingManager
    {
        private readonly ILogger<MessagingManager> logger;
        private readonly IMqttClientService mqttClientService;
        private const string subscribeRequestTopic = "subscribeRequest";
        private Dictionary<string, dynamic> subscriptions = new Dictionary<string, dynamic>();
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

        private async void MqttClientService_MessageReceived(object? sender, MQTTnet.Client.MqttApplicationMessageReceivedEventArgs e)
        {
            if (e.ApplicationMessage.Topic == subscribeRequestTopic)
            {
                try
                {
                    var subscribeTopic = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                    await mqttClientService.Subscribe(subscribeTopic);
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, $"Parsing subscribeRequestTopic not successful!");
                }
            }

            foreach (var subscription in subscriptions)
            {
                if (e.ApplicationMessage.Topic == GetRequestTopic(subscription.Key))
                {
                    try
                    {
                        var jsonString = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

                        var options = new JsonSerializerOptions();
                        options.PropertyNameCaseInsensitive = true;

                        Type genericType = subscription.Value.GetType();

                        if (genericType != null)
                        {
                            var deserializeMethod = typeof(JsonSerializer)
                                .GetMethod(nameof(JsonSerializer.Deserialize), new[] { typeof(string), typeof(Type), typeof(JsonSerializerOptions) });

                            object obj = deserializeMethod.Invoke(null, new object[] { jsonString, genericType, options });

                            if (obj is not null)
                            {
                                RequestReceived?.Invoke(this, obj);
                            }
                        }
                        else
                        {
                            logger.LogError("Invalid subscription type");
                        }
                        //Type T = subscription.Value.GetType();
                        //var payload = System.Text.Json.JsonSerializer.Deserialize<T>()


                        //Type genericClass = typeof(val);
                        ////Type genericClass = typeof(Generic<>);
                        ////// MakeGenericType is badly named
                        //Type constructedClass = genericClass.MakeGenericType(typeof(subscription.));

                        //var tpe = subscription.GetType().GetGenericArguments();


                        // MAQ Sample
                        //IpcMessageConfig messageConfig = GetMessageConfig(tagDefinition);
                        //Type dataType = GetTypeFromName(messageConfig);
                        //if (dataType == null)
                        //{
                        //    appLogger.Error($"Could not parse received message. Did not found the configured datatype {messageConfig.MessageDatatypeName}");
                        //    return "";
                        //}

                        //object parsedMessage = typeof(MaqIpcData)
                        //        .GetMethod(nameof(MaqIpcData.GetReceivedValue), new Type[] { typeof(byte[]) })
                        //        .MakeGenericMethod(dataType)
                        //        .Invoke(this, new object[] { messageData });

                        //return parsedMessage;


                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, $"Parsing MQTT-Message for {e.ApplicationMessage.Topic} not successful!");
                    }
                }
            }

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
            await mqttClientService.Subscribe(subscribeRequestTopic);
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
        public async Task SendMessageRequest()
        {
            await SendMessageRequest(new Payload());
        }


        public async Task SendMessageResponse(Payload payload)
        {
            await mqttClientService.PublishMessage(responseTopic, payload);
        }

        public async Task SendMessageRequest<T>(T payload, string exchangeName)
        {
            string reqTopic = GetRequestTopic(exchangeName);
            await mqttClientService.PublishMessage(subscribeRequestTopic, reqTopic);
            await mqttClientService.PublishMessage(reqTopic, payload);

            if (!subscriptions.ContainsKey(exchangeName))
            {
                subscriptions.Add(exchangeName, payload);
            }
        }

        private string GetRequestTopic(string exchangeName)
        {
            return string.Concat(exchangeName, "_request");
        }
        private string GetResponseTopic(string exchangeName)
        {
            return string.Concat(exchangeName, "_response");
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
    public interface IMessagingManager
    {
        event EventHandler<Payload> RequestReceived;
        event EventHandler<Payload> ResponseReceived;


        Task SendMessageRequest(Payload payload);
        Task SendMessageRequest();
        Task SendMessageRequest<T>(T payload, string exchangeName);
        Task SendMessageResponse(Payload payload);

    }

}
