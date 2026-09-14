namespace MqttServices.Core.Discovery;

public interface IDiscoveryClient
{
    Task SendBroadcastDiscoveryRequest();
    void UpdateSetting(string key, string value);
}