namespace MqttServices.Core.Common;

public class Payload
{
    public string? ExchangeName { get; set; }
    public object Value { get; set; }
    public string ValueType { get; set; }

    public Payload(string? exchangeName, object value)
    {
        ExchangeName = exchangeName;
        Value = value;
        ValueType = value?.GetType().AssemblyQualifiedName;
    }
}
