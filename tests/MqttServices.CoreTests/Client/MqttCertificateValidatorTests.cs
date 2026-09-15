using Microsoft.Extensions.Logging;
using MqttServices.Core.Client;
using System;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace MqttServices.Core.Client.Tests
{
    [TestClass()]
    public class MqttCertificateValidatorTests
    {
        private static X509Certificate2 CreateCertificate(string subject = "CN=broker.test")
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        }

        private static string ThumbprintOf(X509Certificate2 certificate)
        {
            return certificate.GetCertHashString(HashAlgorithmName.SHA256);
        }

        private static MqttCertificateValidator ValidatorFor(MqttClientSettings settings)
        {
            return new MqttCertificateValidator(settings, logger: null);
        }

        // --- Tier 3: no configuration, full validation -----------------------------------

        [TestMethod()]
        public void WithoutConfiguration_AcceptsCertificateWithoutPolicyErrors()
        {
            using var certificate = CreateCertificate();
            var validator = ValidatorFor(new MqttClientSettings());

            Assert.IsTrue(validator.IsAcceptable(certificate, SslPolicyErrors.None));
        }

        [TestMethod()]
        public void WithoutConfiguration_RejectsChainError()
        {
            using var certificate = CreateCertificate();
            var validator = ValidatorFor(new MqttClientSettings());

            Assert.IsFalse(validator.IsAcceptable(certificate, SslPolicyErrors.RemoteCertificateChainErrors));
        }

        [TestMethod()]
        public void WithoutConfiguration_RejectsNameMismatch()
        {
            using var certificate = CreateCertificate();
            var validator = ValidatorFor(new MqttClientSettings());

            Assert.IsFalse(validator.IsAcceptable(certificate, SslPolicyErrors.RemoteCertificateNameMismatch));
        }

        // --- Tier 2: pinning --------------------------------------------------------------

        [TestMethod()]
        public void WithPinnedThumbprint_AcceptsMatchingCertificateDespitePolicyErrors()
        {
            using var certificate = CreateCertificate();
            var validator = ValidatorFor(new MqttClientSettings
            {
                TrustedCertificateThumbprint = ThumbprintOf(certificate)
            });

            // A self-signed broker certificate for CN=localhost always produces both errors;
            // pinning has to accept it anyway, that is the whole point of the tier.
            Assert.IsTrue(validator.IsAcceptable(
                certificate,
                SslPolicyErrors.RemoteCertificateChainErrors | SslPolicyErrors.RemoteCertificateNameMismatch));
        }

        [TestMethod()]
        public void WithPinnedThumbprint_RejectsDifferentCertificate()
        {
            using var pinned = CreateCertificate();
            using var attacker = CreateCertificate();
            var validator = ValidatorFor(new MqttClientSettings
            {
                TrustedCertificateThumbprint = ThumbprintOf(pinned)
            });

            // The man-in-the-middle case: a valid certificate, just not the expected one.
            Assert.IsFalse(validator.IsAcceptable(attacker, SslPolicyErrors.None));
        }

        [TestMethod()]
        public void WithPinnedThumbprint_IgnoresSeparatorsAndCasing()
        {
            using var certificate = CreateCertificate();
            var raw = ThumbprintOf(certificate);

            // How the Windows certificate dialog hands it over: lower case, space separated and
            // with an invisible left-to-right mark in front.
            var windowsStyle = "‎" + string.Join(" ", Chunk(raw.ToLowerInvariant(), 2));
            // How OpenSSL hands it over.
            var openSslStyle = string.Join(":", Chunk(raw, 2));

            foreach (var configured in new[] { windowsStyle, openSslStyle, raw.ToLowerInvariant() })
            {
                var validator = ValidatorFor(new MqttClientSettings
                {
                    TrustedCertificateThumbprint = configured
                });

                Assert.IsTrue(
                    validator.IsAcceptable(certificate, SslPolicyErrors.RemoteCertificateChainErrors),
                    $"thumbprint shape was not accepted: {configured}");
            }
        }

        [TestMethod()]
        public void WithMalformedThumbprint_RejectsEvenTheMatchingCertificate()
        {
            using var certificate = CreateCertificate();
            var truncated = ThumbprintOf(certificate).Substring(0, 40);
            var validator = ValidatorFor(new MqttClientSettings
            {
                TrustedCertificateThumbprint = truncated
            });

            // Fails closed: a typo in the configuration must not degrade into accepting anything,
            // and it must not silently look like a mismatch either.
            Assert.IsFalse(validator.IsAcceptable(certificate, SslPolicyErrors.None));
        }

        [TestMethod()]
        public void WithPinnedThumbprint_RejectsMissingCertificate()
        {
            using var certificate = CreateCertificate();
            var validator = ValidatorFor(new MqttClientSettings
            {
                TrustedCertificateThumbprint = ThumbprintOf(certificate)
            });

            Assert.IsFalse(validator.IsAcceptable(null, SslPolicyErrors.RemoteCertificateNotAvailable));
        }

        // --- Tier 1: the escape hatch -----------------------------------------------------

        [TestMethod()]
        public void WithAllowUntrusted_AcceptsAnything()
        {
            using var certificate = CreateCertificate();
            var validator = ValidatorFor(new MqttClientSettings { AllowUntrustedCertificates = true });

            Assert.IsTrue(validator.IsAcceptable(certificate, SslPolicyErrors.RemoteCertificateChainErrors));
            Assert.IsTrue(validator.IsAcceptable(null, SslPolicyErrors.RemoteCertificateNotAvailable));
        }

        [TestMethod()]
        public void AllowUntrusted_WinsOverPinning()
        {
            using var pinned = CreateCertificate();
            using var other = CreateCertificate();
            var validator = ValidatorFor(new MqttClientSettings
            {
                AllowUntrustedCertificates = true,
                TrustedCertificateThumbprint = ThumbprintOf(pinned)
            });

            // Documents the precedence: the explicit escape hatch is the stronger statement.
            Assert.IsTrue(validator.IsAcceptable(other, SslPolicyErrors.None));
        }

        // --- Normalisation ----------------------------------------------------------------

        [TestMethod()]
        public void NormalizeThumbprint_StripsEverythingButHexDigits()
        {
            Assert.AreEqual("AB12CD", MqttCertificateValidator.NormalizeThumbprint("ab:12:cd"));
            Assert.AreEqual("AB12CD", MqttCertificateValidator.NormalizeThumbprint(" AB 12 CD "));
            Assert.AreEqual("AB12CD", MqttCertificateValidator.NormalizeThumbprint("‎ab-12-cd"));
            Assert.AreEqual("", MqttCertificateValidator.NormalizeThumbprint(null));
            Assert.AreEqual("", MqttCertificateValidator.NormalizeThumbprint(""));
        }

        private static IEnumerable<string> Chunk(string value, int size)
        {
            for (var i = 0; i < value.Length; i += size)
            {
                yield return value.Substring(i, Math.Min(size, value.Length - i));
            }
        }
    }
}
