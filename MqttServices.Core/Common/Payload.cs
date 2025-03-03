namespace MqttServices.Core.Common;

public class Payload
{
    public string? ExchangeName { get; set; }
    //private object _value;
    public object Value { get;  set; }
    public string? ValueType { get;  set; }
    public Guid MessageId { get; set; }
    public RequestType RequestType { get; set; }

    public Payload(string? exchangeName, object value)
    {
        ExchangeName = exchangeName;
        Value = value;
        ValueType = value?.GetType().AssemblyQualifiedName;
        MessageId = Guid.NewGuid();
        RequestType = RequestType.Generic;
    }

    public Payload(string? exchangeName, object value, Guid messageId)
    {
        ExchangeName = exchangeName;
        Value = value;
        ValueType = value?.GetType().AssemblyQualifiedName;
        MessageId = messageId;
        RequestType = RequestType.Generic;
    }

    public Payload(string? exchangeName, object value, Guid messageId, RequestType requestType)
    {
        ExchangeName = exchangeName;
        Value = value;
        ValueType = value?.GetType().AssemblyQualifiedName;
        MessageId = messageId;
        RequestType = requestType;
    }

    public Payload()
    {
        RequestType = RequestType.Generic;
        ValueType = Value?.GetType().AssemblyQualifiedName;
    }

    public void SetValue(object value)
    {
        Value = value;
        ValueType = value?.GetType().AssemblyQualifiedName;
    }
}

//public record TransferRecord(Guid MessageId, object value);

