namespace MqttServices.Core.Common;

public class Payload
{
    public string? ExchangeName { get; set; }
    public object Value { get; set; }
    //public string ValueType
    //{
    //    get
    //    {
    //        return Value?.GetType().AssemblyQualifiedName ?? string.Empty;
    //    }
    //}
    public string? ValueType { get; set; }
    public Guid MessageId { get; set; }
    public RequestType RequestType { get; set; }
    public Payload(string? exchangeName, object value)
    {
        ExchangeName = exchangeName;
        Value = value;
        ValueType = value?.GetType().AssemblyQualifiedName;
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
        ValueType = Value?.GetType().AssemblyQualifiedName;
        RequestType = RequestType.Generic;
    }
}

//public record TransferRecord(Guid MessageId, object value);

