using MqttServices.Core.Common;

namespace MqttServices.Core.Services;

public interface IMessagingManager
{
    event EventHandler<Payload> MessageReceived;
    event EventHandler<Payload> RequestReceived;
    event EventHandler<Payload> RequestForGetOneReceived;
    event EventHandler<Payload> RequestForGetAllReceived;
    event EventHandler<Payload> RequestForUpsertReceived;
    event EventHandler<Payload> RequestForUpdateReceived;
    event EventHandler<Payload> RequestForInsertReceived;
    event EventHandler<Payload> RequestForDeleteReceived;
    event EventHandler<Payload> ResponseReceived;

    Task SendMessage<T>(T payload, string exchangeName);
    Task<Guid> SendMessageRequest<T>(T payload, string exchangeName);

    /// <summary>
    /// Sends a message using default exchange name, which will be derived from the type of the payload
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="payload"></param>
    /// <returns></returns>
    Task SendMessage<T>(T payload);

    /// <summary>
    /// Sends a message request using default exchange name, which will be derived from the type of the payload,
    /// expecting answer in ResponseReceived-Event
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="payload"></param>
    /// <returns></returns>
    Task<Guid> SendMessageRequest<T>(T payload);
    Task<Guid> SendMessageRequest<T>(T payload, RequestType requestType);
    Task<Guid> SendMessageRequest(string exchangeName, RequestType requestType);


    /// <summary>
    /// Sends a message request, just to trigger something on the other side without expecting an answer
    /// </summary>
    /// <param name="exchangeName"></param>
    /// <returns></returns>
    Task<Guid> SendMessageRequest(string exchangeName);

    /// <summary>
    /// Sends a respond message to a request
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="payload"></param>
    /// <param name="exchangeName"></param>
    /// <returns></returns>
    Task SendMessageResponse<T>(T payload, string exchangeName);
    Task SendMessageResponse(Payload payload);
    Task PublishMqttMessage(string topic, object payload);

}
