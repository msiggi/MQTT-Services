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
        //[TestMethod()]
        //public void ValueType_IsUpdated_WhenValueChanges()
        //{
        //    // Arrange
        //    var payload = new Payload();
        //    var initialObject = new { Name = "Initial" };
        //    var newObject = new { Name = "New" };

        //    // Act
        //    payload.Value = initialObject;
        //    var initialType = payload.ValueType;

        //    payload.Value = newObject;
        //    var newType = payload.ValueType;

        //    // Assert
        //    Assert.AreEqual(initialObject.GetType().AssemblyQualifiedName, initialType);
        //    Assert.AreEqual(newObject.GetType().AssemblyQualifiedName, newType);
        //}

        internal class Person
        {
            public string Name { get; set; }
            public DateTime Birthday { get; set; }
        }
    }
}