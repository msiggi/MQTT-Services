namespace SampleCommon
{
    public class AddressDataRequest
    {
        public int CityId { get; set; }
    }
    public class AddressDataResponse
    {
        public AddressData AddressData { get; set; }
    }

    public class AddressData
    {
        public int CityId { get; set; }
        public string CityName { get; set; }
        public string Street { get; set; }
        public int Number { get; set; }

    }
}
