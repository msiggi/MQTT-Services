using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;

namespace MqttServices.Core.Client
{
    public interface IMqttClientService
    {
        event EventHandler<MqttClientConnectedEventArgs>? ClientConnected;
        event EventHandler<ConnectingFailedEventArgs>? ClientConnectionFailed;
        event EventHandler<MqttApplicationMessageReceivedEventArgs>? MessageReceived;
        bool IsConnected { get; set; }

        Task Connect();
        void Dispose();
        Task PublishMessage(string topic, object payload);
        Task PublishMessage(string topic, string payload);
        Task Subscribe(string topic);
    }
}