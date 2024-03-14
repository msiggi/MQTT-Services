using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MqttServices.Core.Client;
using MqttServices.Core.Common;
using MqttServices.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MqttServices.Core.Services.Tests
{
    [TestClass()]
    public class MessagingManagerTests
    {
        private MessagingManager messagingManager;

        public MessagingManagerTests() 
        {
            messagingManager = new MessagingManager(A.Fake<ILogger<MessagingManager>>(), null);
        }

        [TestMethod()]
        public void DeserializePayloadObjectTest()
        {
            Person person = new Person { Name = "Max", Birthday = DateTime.Now.AddYears(-40) };
            Payload payload = new Payload("testexchange", person);

            var serializedPayload = JsonSerializer.Serialize(payload);
            Payload transferedPayload = messagingManager.DeserializePayloadObject(Encoding.ASCII.GetBytes(serializedPayload));

            Assert.IsTrue(transferedPayload is not null);
            Assert.IsTrue(((Person)transferedPayload.Value).Name == person.Name);
        }

    internal class Person
    {
        public string Name { get; set; }
        public DateTime Birthday { get; set; }
    }
}
}