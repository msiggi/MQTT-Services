namespace MqttServices.Core.Client;

public class MqttClientSettings
{
    public bool Enabled { get; set; } = true;
    public string ServiceName { get; set; } = "";
    public string BrokerHost { get; set; } = "";
    public int BrokerPort { get; set; }
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public bool SerializeWithCamelCase { get; set; } = false;
    public bool IgnoreCycles { get; set; } = true;
}
