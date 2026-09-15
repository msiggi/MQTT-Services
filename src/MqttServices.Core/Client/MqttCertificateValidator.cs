using Microsoft.Extensions.Logging;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace MqttServices.Core.Client;

/// <summary>
/// Decides whether the certificate a broker presents during the TLS handshake is acceptable.
/// <para>
/// Three tiers, in this order of precedence:
/// <list type="number">
/// <item><see cref="MqttClientSettings.AllowUntrustedCertificates"/> accepts anything.</item>
/// <item><see cref="MqttClientSettings.TrustedCertificateThumbprint"/> accepts exactly the
/// certificate with that SHA-256 fingerprint ("pinning"), regardless of chain or host name.</item>
/// <item>Neither set: the certificate must pass the platform's full validation.</item>
/// </list>
/// </para>
/// </summary>
public sealed class MqttCertificateValidator
{
    private readonly MqttClientSettings settings;
    private readonly ILogger? logger;

    public MqttCertificateValidator(MqttClientSettings settings, ILogger? logger = null)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.logger = logger;
    }

    /// <summary>
    /// Strips everything that is not a hex digit and upper-cases the rest, so that the
    /// fingerprint may be configured in whatever shape it was copied from: with colons
    /// (OpenSSL), with spaces (the Windows certificate dialog, which also prefixes an
    /// invisible left-to-right mark) or as plain hex.
    /// </summary>
    public static string NormalizeThumbprint(string? thumbprint)
    {
        if (string.IsNullOrEmpty(thumbprint))
        {
            return "";
        }

        var normalized = new StringBuilder(thumbprint.Length);
        foreach (var c in thumbprint)
        {
            if (char.IsAsciiHexDigit(c))
            {
                normalized.Append(char.ToUpperInvariant(c));
            }
        }

        return normalized.ToString();
    }

    /// <summary>
    /// The decision itself, separated from the MQTTnet event args so that it can be tested
    /// without a TLS handshake.
    /// </summary>
    /// <param name="certificate">The certificate the broker presented, if any.</param>
    /// <param name="sslPolicyErrors">What the platform's own validation made of it.</param>
    public bool IsAcceptable(X509Certificate? certificate, SslPolicyErrors sslPolicyErrors)
    {
        if (settings.AllowUntrustedCertificates)
        {
            // Logged every time on purpose: this is the tier that silently disables the
            // protection, so it should be visible in the log of every connection attempt.
            logger?.LogWarning(
                "Accepting the broker certificate of {Host} without validation because " +
                "AllowUntrustedCertificates is set. The connection is encrypted but not " +
                "protected against a man-in-the-middle.",
                settings.BrokerHost);
            return true;
        }

        var expected = NormalizeThumbprint(settings.TrustedCertificateThumbprint);
        if (expected.Length > 0)
        {
            return IsPinnedCertificate(certificate, expected);
        }

        if (sslPolicyErrors == SslPolicyErrors.None)
        {
            return true;
        }

        LogRejection(certificate, sslPolicyErrors);
        return false;
    }

    private bool IsPinnedCertificate(X509Certificate? certificate, string expected)
    {
        // A SHA-256 fingerprint is 32 bytes, so 64 hex digits. Anything else is a typo in the
        // configuration, and silently never matching would be a miserable thing to debug.
        if (expected.Length != 64)
        {
            logger?.LogError(
                "TrustedCertificateThumbprint is not a SHA-256 fingerprint: expected 64 hex " +
                "digits, got {Length}. Rejecting the connection to {Host}.",
                expected.Length, settings.BrokerHost);
            return false;
        }

        if (certificate is null)
        {
            logger?.LogError("The broker {Host} presented no certificate.", settings.BrokerHost);
            return false;
        }

        // An ordinary comparison is correct here. The fingerprint is public information, not a
        // secret, so there is nothing for a timing attack to learn.
        var actual = certificate.GetCertHashString(HashAlgorithmName.SHA256);
        if (string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        logger?.LogError(
            "The certificate of broker {Host} does not match the pinned fingerprint. " +
            "Expected {Expected}, got {Actual} (subject {Subject}).",
            settings.BrokerHost, expected, actual, certificate.Subject);
        return false;
    }

    private void LogRejection(X509Certificate? certificate, SslPolicyErrors sslPolicyErrors)
    {
        var subject = certificate?.Subject ?? "<none>";

        // The name mismatch is by far the most common cause, and the least obvious one, so it
        // gets its own message naming the way out.
        if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch))
        {
            logger?.LogError(
                "The certificate of broker {Host} is not valid for that name (subject {Subject}). " +
                "Connect using the host name the certificate was issued for, or pin the " +
                "certificate via TrustedCertificateThumbprint.",
                settings.BrokerHost, subject);
        }

        if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors))
        {
            logger?.LogError(
                "The certificate chain of broker {Host} could not be verified (subject {Subject}, " +
                "SHA-256 {Thumbprint}). A self-signed broker certificate has to be pinned via " +
                "TrustedCertificateThumbprint.",
                settings.BrokerHost,
                subject,
                certificate?.GetCertHashString(HashAlgorithmName.SHA256) ?? "<none>");
        }

        if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable))
        {
            logger?.LogError("The broker {Host} presented no certificate.", settings.BrokerHost);
        }
    }
}
