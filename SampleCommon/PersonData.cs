namespace SampleCommon
{
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
    }
}
