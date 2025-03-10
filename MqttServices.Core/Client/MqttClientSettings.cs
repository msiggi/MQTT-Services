namespace MqttServices.Core.Client;

public class MqttClientSettings
{
    public bool Enabled { get; set; } = true;
    public string ApplicationKey { get; set; } = "";
    public string ServiceName { get; set; } = "";
    public string BrokerHost { get; set; } = "";
    public int BrokerPort { get; set; }
    public int Timeout { get; set; } = 10000;
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public bool SerializeWithCamelCase { get; set; } = false;
    public bool IgnoreCycles { get; set; } = true;
    public string TlsVersion { get; set; } = "1.2";
    public bool EncryptWithTls { get; set; } = true;
}
