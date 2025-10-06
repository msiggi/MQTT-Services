namespace MqttServices.Core.Client;

public class MqttClientSettings
{
    /// <summary>
    /// Enabled or disabled the MqttClientService
    /// </summary>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Must be unique for all applications which must call to each other
    /// </summary>
    public string ApplicationKey { get; set; } = "";
    /// <summary>
    /// The name of the service for the MQTT-Client
    /// </summary>
    public string ServiceName { get; set; } = "";
    public string BrokerHost { get; set; } = "";
    public int BrokerPort { get; set; }
    public int Timeout { get; set; } = 10000;
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public string TlsVersion { get; set; } = "1.2";
    public bool EncryptWithTls { get; set; } = true;
    public DiscoverySettings Discovery { get; set; } = new();

}
public class DiscoverySettings
{
    public bool SearchForDiscoveryServer { get; set; } = false;
    public int Port { get; set; } = 5005;
}