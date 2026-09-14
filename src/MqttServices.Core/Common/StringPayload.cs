namespace MqttServices.Core.Common
{
    public class StringPayload
    {
        public string ExchangeName { get; set; }
        public string StringValue { get; set; }
        public Guid MessageId { get; set; }

        public StringPayload(string exchangeName, string stringValue)
        {
            ExchangeName = exchangeName;
            StringValue = stringValue;
            MessageId = Guid.NewGuid();
        }
        public StringPayload()
        {
            MessageId = Guid.NewGuid();
        }
    }

}