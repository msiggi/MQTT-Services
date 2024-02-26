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
    Response = 1
}
public class PersonDataRequest
{
    public int PersonId { get; set; }
}

public class PersonDataResponse
{
    public int PersonId { get; set; }
    public string Name { get; set; }
    public DateTime Birthday { get; set; }
}