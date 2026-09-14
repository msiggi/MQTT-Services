namespace SampleCommon;

public class GuitarPlayer
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime BirthDate { get; set; }
    public List<Guitar> OwnedGuitars { get; set; } = new List<Guitar>();
}
public class Guitar
{
    public string Brand { get; set; }
    public string Model { get; set; }
    public string Color { get; set; }
    public GuitarPlayer? Owner { get; set; }
}
