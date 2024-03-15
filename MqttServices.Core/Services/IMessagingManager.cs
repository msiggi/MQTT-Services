using MqttServices.Core.Common;

namespace MqttServices.Core.Services;

public interface IMessagingManager
{
    event EventHandler<Payload> MessageReceived;
    event EventHandler<Payload> RequestReceived;
    event EventHandler<Payload> ResponseReceived;

    Task SendMessage<T>(T payload, string exchangeName);
    Task SendMessageRequest<T>(T payload, string exchangeName);
    Task SendMessageResponse<T>(T payload, string exchangeName);
}
