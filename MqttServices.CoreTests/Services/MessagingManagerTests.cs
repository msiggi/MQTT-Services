using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MqttServices.Core.Client;
using MqttServices.Core.Common;
using MqttServices.Core.Services;
using SampleCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static MqttServices.Core.Services.Tests.MessagingManagerTests;

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
        public void DeserializePayloadObject_Works()
        {
            Person person = new Person { Name = "Max", Birthday = DateTime.Now.AddYears(-40) };
            Payload payload = new Payload("testexchange", person);

            var serializedPayload = JsonSerializer.Serialize(payload);
            Payload transferedPayload = messagingManager.DeserializePayloadObject(Encoding.ASCII.GetBytes(serializedPayload));

            Assert.IsTrue(transferedPayload is not null);
            Assert.IsTrue(((Person)transferedPayload.Value).Name == person.Name);
        }

        [TestMethod()]
        public void DeserializePayloadObject_WorksWithList()
        {
            var guitarPlayers = new List<GuitarPlayer>
            {
                new GuitarPlayer { Name = "Jimi Hendrix", OwnedGuitars = new List<Guitar> { new Guitar { Model = "Stratocaster", Brand = "Fender", Color = "Sunburst" } } },
                new GuitarPlayer { Name = "Eric Clapton", OwnedGuitars = new List<Guitar> { new Guitar { Model = "Stratocaster", Brand = "Fender", Color = "Black" } } },
                new GuitarPlayer { Name = "Jimmy Page", OwnedGuitars = new List<Guitar> { new Guitar { Model = "Les Paul", Brand = "Gibson", Color = "Sunburst" } } },
                new GuitarPlayer { Name = "David Gilmour", OwnedGuitars = new List<Guitar> { new Guitar { Model = "Stratocaster", Brand = "Fender", Color = "Black" } } }
            };

            Payload payload = new Payload("testexchange", guitarPlayers, Guid.NewGuid());

            var serializedPayload = JsonSerializer.Serialize(payload);
            Payload transferedPayload = messagingManager.DeserializePayloadObject(Encoding.ASCII.GetBytes(serializedPayload));

            Assert.IsTrue(transferedPayload is not null);
            Assert.IsTrue(((List<GuitarPlayer>)transferedPayload.Value).Count == guitarPlayers.Count);
        }

        [TestMethod()]
        public void DeserializePayloadObject_WorksWithListAsByteArray()
        {
            byte[] byteArray = new byte[]
            {
                123, 13, 10, 32, 32, 34, 69, 120, 99, 104, 97, 110, 103, 101, 78, 97, 109, 101, 34, 58, 32, 34, 103, 112, 108, 97, 121, 101, 114, 115, 69, 120, 34, 44, 13, 10, 32, 32, 34, 86, 97, 108, 117, 101, 34, 58, 32, 110, 117, 108, 108, 44, 13, 10, 32, 32, 34, 86, 97, 108, 117, 101, 84, 121, 112, 101, 34, 58, 32, 110, 117, 108, 108, 44, 13, 10, 32, 32, 34, 77, 101, 115, 115, 97, 103, 101, 73, 100, 34, 58, 32, 34, 56, 100, 57, 98, 54, 97, 97, 48, 45, 55, 53, 51, 52, 45, 52, 54, 55, 102, 45, 56, 98, 100, 52, 45, 55, 55, 53, 100, 100, 48, 97, 102, 99, 48, 48, 100, 34, 44, 13, 10, 32, 32, 34, 82, 101, 113, 117, 101, 115, 116, 84, 121, 112, 101, 34, 58, 32, 50, 13, 10, 125
            };
            Payload transferedPayload = messagingManager.DeserializePayloadObject(byteArray);

            Assert.IsTrue(transferedPayload is not null);
            Assert.IsTrue(((List<GuitarPlayer>)transferedPayload.Value).Count == 4);
        }

        [TestMethod()]
        public void DeserializePayloadObject_WorksWithByteArray()
        {
            byte[] byteArray = new byte[]
            {
                123, 13, 10, 32, 32, 34, 69, 120, 99, 104, 97, 110, 103, 101, 78, 97, 109, 101, 34, 58, 32, 34, 116, 101, 115, 116, 69, 120, 99, 104, 97, 110, 103, 101, 34, 44, 13, 10, 32, 32, 34, 86, 97, 108, 117, 101, 34, 58, 32, 123, 13, 10, 32, 32, 32, 32, 34, 78, 97, 109, 101, 34, 58, 32, 34, 84, 101, 115, 116, 49, 34, 13, 10, 32, 32, 125, 44, 13, 10, 32, 32, 34, 86, 97, 108, 117, 101, 84, 121, 112, 101, 34, 58, 32, 34, 92, 117, 48, 48, 51, 67, 92, 117, 48, 48, 51, 69, 102, 95, 95, 65, 110, 111, 110, 121, 109, 111, 117, 115, 84, 121, 112, 101, 48, 92, 117, 48, 48, 54, 48, 49, 91, 91, 83, 121, 115, 116, 101, 109, 46, 83, 116, 114, 105, 110, 103, 44, 32, 83, 121, 115, 116, 101, 109, 46, 80, 114, 105, 118, 97, 116, 101, 46, 67, 111, 114, 101, 76, 105, 98, 44, 32, 86, 101, 114, 115, 105, 111, 110, 61, 57, 46, 48, 46, 48, 46, 48, 44, 32, 67, 117, 108, 116, 117, 114, 101, 61, 110, 101, 117, 116, 114, 97, 108, 44, 32, 80, 117, 98, 108, 105, 99, 75, 101, 121, 84, 111, 107, 101, 110, 61, 55, 99, 101, 99, 56, 53, 100, 55, 98, 101, 97, 55, 55, 57, 56, 101, 93, 93, 44, 32, 83, 97, 109, 112, 108, 101, 67, 108, 105, 101, 110, 116, 77, 101, 115, 115, 97, 103, 105, 110, 103, 49, 44, 32, 86, 101, 114, 115, 105, 111, 110, 61, 49, 46, 48, 46, 48, 46, 48, 44, 32, 67, 117, 108, 116, 117, 114, 101, 61, 110, 101, 117, 116, 114, 97, 108, 44, 32, 80, 117, 98, 108, 105, 99, 75, 101, 121, 84, 111, 107, 101, 110, 61, 110, 117, 108, 108, 34, 44, 13, 10, 32, 32, 34, 77, 101, 115, 115, 97, 103, 101, 73, 100, 34, 58, 32, 34, 48, 48, 48, 48, 48, 48, 48, 48, 45, 48, 48, 48, 48, 45, 48, 48, 48, 48, 45, 48, 48, 48, 48, 45, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 34, 44, 13, 10, 32, 32, 34, 82, 101, 113, 117, 101, 115, 116, 84, 121, 112, 101, 34, 58, 32, 48, 13, 10, 125
            };
            Payload transferedPayload = messagingManager.DeserializePayloadObject(byteArray);

            Assert.IsTrue(transferedPayload is not null);
            Assert.IsTrue(((GuitarPlayer)transferedPayload.Value).Name.Contains("Hendrix"));
        }

        [TestMethod()]
        public void DeserializePayloadObject_ContainsGuid()
        {
            Guid guid = Guid.NewGuid();
            Person person = new Person { Name = "Max", Birthday = DateTime.Now.AddYears(-40) };
            Payload payload = new Payload("testexchange", person, guid);

            var serializedPayload = JsonSerializer.Serialize(payload);
            Payload transferedPayload = messagingManager.DeserializePayloadObject(Encoding.ASCII.GetBytes(serializedPayload));

            Assert.IsTrue(transferedPayload is not null);
            Assert.IsTrue(transferedPayload.MessageId == guid);
        }
        [TestMethod()]
        public void DeserializePayloadObject_ContainsRequestType()
        {
            Guid guid = Guid.NewGuid();
            Person person = new Person { Name = "Max", Birthday = DateTime.Now.AddYears(-40) };
            Payload payload = new Payload("testexchange", person, guid, RequestType.Upsert);

            var serializedPayload = JsonSerializer.Serialize(payload);
            Payload transferedPayload = messagingManager.DeserializePayloadObject(Encoding.ASCII.GetBytes(serializedPayload));

            Assert.IsTrue(transferedPayload is not null);
            Assert.IsTrue(transferedPayload.RequestType == payload.RequestType);
        }

        internal class Person
        {
            public string Name { get; set; }
            public DateTime Birthday { get; set; }
        }
    }
}