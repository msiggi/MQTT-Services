namespace MqttServices.Core.Common;

public class Payload
{
    public string? ExchangeName { get; set; }
    public object Value { get; set; }
    public string ValueType { get; set; }
    public Guid MessageId { get;  set; }
    public Payload(string? exchangeName, object value)
    {
        ExchangeName = exchangeName;
        Value = value;
        ValueType = value?.GetType().AssemblyQualifiedName;
    }
    public Payload(string? exchangeName, object value, Guid messageId)
    {
        ExchangeName = exchangeName;
        Value = value;
        ValueType = value?.GetType().AssemblyQualifiedName;
        MessageId = messageId;
    }
    public Payload()
    {

    }
}

public record TransferRecord(Guid MessageId, object value);

