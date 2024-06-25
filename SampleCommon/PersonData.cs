namespace SampleCommon;

public class PersonDataRequest
{
    public int PersonId { get; set; }
}
public class PersonDataResponse
{
    public PersonData PersonData { get; set; }
}

public class PersonData
{
    public int PersonId { get; set; }
    public string Name { get; set; }
    public DateTime Birthday { get; set; }
    public List<OwnedCar> OwnedCars { get; set; } = new List<OwnedCar>();
}
public class OwnedCar
{
    public string Brand { get; set; }
    public string Model { get; set; }
    public int Year { get; set; }
    public PersonData? Owner { get; set; }
}
