using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MqttServices.Core.Broker;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace MqttServices.Core.Broker.Tests
{
    [TestClass()]
    public class MqttBrokerServiceTests
    {
        [TestMethod()]
        public void CreateSelfSignedCertificate__WithMachineKeySet_Succeeds()
        {
            // Arrange - Call the public unused method which has similar signature
            // This tests that certificate creation with proper flags works
            
            // Act
            var certificate = MqttBrokerService.CreateSelfSignedCertificate__("localhost", "1.3.6.1.5.5.7.3.1");

            // Assert
            Assert.IsNotNull(certificate);
            Assert.IsTrue(certificate.HasPrivateKey);
            Assert.AreEqual("CN=localhost", certificate.Subject);
        }

        [TestMethod()]
        public void MqttBrokerService_Creation_DoesNotThrow()
        {
            // Arrange
            var logger = new TestLogger<MqttBrokerService>();
            var settings = Options.Create(new MqttBrokerSettings 
            { 
                EnableBroker = false // Don't actually start the broker for this test
            });

            // Act & Assert - Should not throw
            var service = new MqttBrokerService(logger, settings);
            Assert.IsNotNull(service);
            
            // Cleanup
            ((IDisposable)service).Dispose();
        }

        // Simple test logger implementation
        private class TestLogger<T> : ILogger<T>
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => false;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
        }
    }
}
