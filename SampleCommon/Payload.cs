namespace SampleCommon;

public class Payload
{
    public string? ExchangeName { get; set; }
    public PayloadType PayloadType { get; set; }
    public object Value { get; set; }
}

public enum PayloadType
{
    Request = 0,
    Resonse = 1
}