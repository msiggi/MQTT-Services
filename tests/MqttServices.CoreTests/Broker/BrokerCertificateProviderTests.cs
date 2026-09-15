using MqttServices.Core.Broker;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace MqttServices.Core.Broker.Tests
{
    [TestClass()]
    public class BrokerCertificateProviderTests
    {
        private string directory = "";

        [TestInitialize()]
        public void CreateTemporaryDirectory()
        {
            directory = Path.Combine(Path.GetTempPath(), "mqtt-cert-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }

        [TestCleanup()]
        public void RemoveTemporaryDirectory()
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        private string PathIn(string fileName) => Path.Combine(directory, fileName);

        private static string ThumbprintOf(X509Certificate2 certificate)
            => certificate.GetCertHashString(HashAlgorithmName.SHA256);

        [TestMethod()]
        public void WithoutPath_GeneratesADifferentCertificateEveryTime()
        {
            // The previous behaviour, kept as the default so existing setups do not change.
            using var first = BrokerCertificateProvider.GetOrCreate(new BrokerCertificateSettings());
            using var second = BrokerCertificateProvider.GetOrCreate(new BrokerCertificateSettings());

            Assert.AreNotEqual(ThumbprintOf(first), ThumbprintOf(second));
        }

        [TestMethod()]
        public void WithPath_CreatesTheFileOnFirstUse()
        {
            var path = PathIn("broker.pfx");
            using var certificate = BrokerCertificateProvider.GetOrCreate(new BrokerCertificateSettings { Path = path });

            Assert.IsTrue(File.Exists(path));
            Assert.IsTrue(certificate.HasPrivateKey);
        }

        [TestMethod()]
        public void WithPath_KeepsTheSameFingerprintAcrossRestarts()
        {
            // The whole point: a pinned client survives a broker restart.
            var settings = new BrokerCertificateSettings { Path = PathIn("broker.pfx") };

            using var firstStart = BrokerCertificateProvider.GetOrCreate(settings);
            using var secondStart = BrokerCertificateProvider.GetOrCreate(settings);

            Assert.AreEqual(ThumbprintOf(firstStart), ThumbprintOf(secondStart));
        }

        [TestMethod()]
        public void WithPassword_RoundTripsTheStoredCertificate()
        {
            var settings = new BrokerCertificateSettings
            {
                Path = PathIn("protected.pfx"),
                Password = "s3cret"
            };

            using var created = BrokerCertificateProvider.GetOrCreate(settings);
            using var loaded = BrokerCertificateProvider.GetOrCreate(settings);

            Assert.AreEqual(ThumbprintOf(created), ThumbprintOf(loaded));
            Assert.IsTrue(loaded.HasPrivateKey);
        }

        [TestMethod()]
        public void WithWrongPassword_RefusesInsteadOfOverwriting()
        {
            var path = PathIn("protected.pfx");
            BrokerCertificateProvider.GetOrCreate(new BrokerCertificateSettings { Path = path, Password = "right" }).Dispose();
            var before = File.ReadAllBytes(path);

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                BrokerCertificateProvider.GetOrCreate(new BrokerCertificateSettings { Path = path, Password = "wrong" }));

            // Silently generating a replacement would destroy a certificate the operator may still
            // need, and every pinned client with it.
            CollectionAssert.AreEqual(before, File.ReadAllBytes(path));
        }

        [TestMethod()]
        public void WithExpiredStoredCertificate_ReplacesIt()
        {
            var path = PathIn("expired.pfx");
            WriteExpiredCertificate(path);
            var expiredThumbprint = ThumbprintOf(X509CertificateLoader.LoadPkcs12FromFile(path, null));

            using var replacement = BrokerCertificateProvider.GetOrCreate(new BrokerCertificateSettings { Path = path });

            Assert.AreNotEqual(expiredThumbprint, ThumbprintOf(replacement));
            Assert.IsTrue(replacement.NotAfter > DateTime.Now);
        }

        [TestMethod()]
        public void CreatesMissingDirectories()
        {
            var path = Path.Combine(directory, "nested", "deeper", "broker.pfx");

            using var certificate = BrokerCertificateProvider.GetOrCreate(new BrokerCertificateSettings { Path = path });

            Assert.IsTrue(File.Exists(path));
        }

        [TestMethod()]
        public void GeneratedCertificate_CarriesTheConfiguredNamesAndAddresses()
        {
            var settings = new BrokerCertificateSettings
            {
                Path = PathIn("named.pfx"),
                SubjectName = "broker.internal",
                HostNames = { "mqtt.example.org", "192.168.1.50" }
            };

            using var certificate = BrokerCertificateProvider.GetOrCreate(settings);
            var names = SubjectAlternativeNamesOf(certificate);

            Assert.AreEqual("CN=broker.internal", certificate.Subject);
            // The subject common name alone is not enough -- validation reads these.
            StringAssert.Contains(names, "broker.internal");
            StringAssert.Contains(names, "mqtt.example.org");
            StringAssert.Contains(names, "192.168.1.50");
            // Always present, so a client on the broker machine keeps working.
            StringAssert.Contains(names, "localhost");
        }

        [TestMethod()]
        public void GeneratedCertificate_IsValidForServerAuthentication()
        {
            using var certificate = BrokerCertificateProvider.GetOrCreate(new BrokerCertificateSettings());

            var enhancedKeyUsage = certificate.Extensions
                .OfType<X509EnhancedKeyUsageExtension>()
                .SelectMany(e => e.EnhancedKeyUsages.Cast<Oid>())
                .Select(o => o.Value)
                .ToList();

            CollectionAssert.Contains(enhancedKeyUsage, "1.3.6.1.5.5.7.3.1");
        }

        [TestMethod()]
        public void RespectsTheConfiguredValidity()
        {
            using var certificate = BrokerCertificateProvider.GetOrCreate(
                new BrokerCertificateSettings { Path = PathIn("short.pfx"), ValidityDays = 30 });

            var days = (certificate.NotAfter - DateTime.Now).TotalDays;
            Assert.IsTrue(days is > 28 and < 31, $"unexpected validity of {days:F1} days");
        }

        private static string SubjectAlternativeNamesOf(X509Certificate2 certificate)
        {
            return string.Join(
                " | ",
                certificate.Extensions
                    .OfType<X509SubjectAlternativeNameExtension>()
                    .SelectMany(e => e.EnumerateDnsNames().Concat(e.EnumerateIPAddresses().Select(a => a.ToString()))));
        }

        private static void WriteExpiredCertificate(string path)
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var expired = request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-10),
                DateTimeOffset.UtcNow.AddDays(-1));

            File.WriteAllBytes(path, expired.Export(X509ContentType.Pfx));
        }
    }
}
