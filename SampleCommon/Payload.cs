namespace SampleCommon;

public class Payload
{
    public string? ExchangeName { get; set; }
    public PayloadType PayloadType { get; set; }
    public object Value { get; set; }
    public string ValueType { get; set; }
}

public enum PayloadType
{
    Request = 0,
    Resonse = 1
}
public class RequestPersonData
{
    public string Name { get; set; }

}

public class ResponsePersonData
{
    public string Name { get; set; }
    public DateTime Birtday { get; set; }
    public string Name { get; set; }

}