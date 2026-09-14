using MQTTnet;

namespace MqttServices.Core.Client;

public interface IMqttClientService
{
    event EventHandler<MqttClientConnectedEventArgs>? ClientConnected;
    event EventHandler<MqttApplicationMessageReceivedEventArgs>? MessageReceived;
    bool IsConnected { get; set; }
    bool IsConnecting { get; set; }
    string ApplicationKey { get; set; }
    Task Connect();
    Task ReConnect(string brokerHost, int brokerPort);
    Task Disconnect();
    void Dispose();
    Task PublishMessage(string topic, object payload);
    Task PublishMessage(string topic, string payload);
    Task Subscribe(string topic);
}