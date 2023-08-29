using MQTTnet.Client;

namespace MqttServices.Core.Client
{
    public interface IMqttClientService
    {
        event EventHandler<MqttClientConnectedEventArgs>? ClientConnected;
        event EventHandler<MqttApplicationMessageReceivedEventArgs>? MessageReceived;
        bool IsConnected { get; set; }

        Task Connect();
        void Dispose();
        Task PublishMessage(string topic, object payload);
        Task Subscribe(string topic);
    }
}