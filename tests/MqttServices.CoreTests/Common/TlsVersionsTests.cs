using MqttServices.Core.Common;
using System.Security.Authentication;

namespace MqttServices.Core.Common.Tests
{
    [TestClass()]
    public class TlsVersionsTests
    {
        [TestMethod()]
        public void Parse_MapsTheSupportedVersions()
        {
            Assert.AreEqual(SslProtocols.Tls, TlsVersions.Parse("1.0"));
            Assert.AreEqual(SslProtocols.Tls11, TlsVersions.Parse("1.1"));
            Assert.AreEqual(SslProtocols.Tls12, TlsVersions.Parse("1.2"));
            Assert.AreEqual(SslProtocols.Tls13, TlsVersions.Parse("1.3"));
        }

        [TestMethod()]
        public void Parse_LeavesTheChoiceToTheSystemWhenNothingIsConfigured()
        {
            // SslProtocols.None is how "no explicit choice" reaches the platform, which then
            // negotiates the highest version both sides support.
            Assert.AreEqual(SslProtocols.None, TlsVersions.Parse(null));
            Assert.AreEqual(SslProtocols.None, TlsVersions.Parse(""));
            Assert.AreEqual(SslProtocols.None, TlsVersions.Parse("   "));
        }

        [TestMethod()]
        public void Parse_AcceptsAutoInAnyCasing()
        {
            Assert.AreEqual(SslProtocols.None, TlsVersions.Parse("auto"));
            Assert.AreEqual(SslProtocols.None, TlsVersions.Parse("Auto"));
            Assert.AreEqual(SslProtocols.None, TlsVersions.Parse("AUTO"));
        }

        [TestMethod()]
        public void Parse_ToleratesSurroundingWhitespace()
        {
            Assert.AreEqual(SslProtocols.Tls13, TlsVersions.Parse(" 1.3 "));
        }

        [TestMethod()]
        public void Parse_FallsBackToTheSystemDefaultOnAnUnknownValue()
        {
            // The old mapping turned anything unknown into TLS 1.2, which hid both the typo and
            // an intended 1.3. Falling back to the system default is the safer of the two.
            Assert.AreEqual(SslProtocols.None, TlsVersions.Parse("Tls12"));
            Assert.AreEqual(SslProtocols.None, TlsVersions.Parse("1.4"));
            Assert.AreEqual(SslProtocols.None, TlsVersions.Parse("nonsense"));
        }

        [TestMethod()]
        public void Parse_DoesNotReturnARetiredProtocolByDefault()
        {
            // Guards the actual point of the default: never silently negotiate SSL 3.0 or below.
            var negotiable = TlsVersions.Parse(null);

            Assert.IsFalse(negotiable.HasFlag(SslProtocols.Ssl2));
            Assert.IsFalse(negotiable.HasFlag(SslProtocols.Ssl3));
        }
    }
}
