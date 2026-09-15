namespace MqttServices.Core.Client;

public class MqttClientSettings
{
    /// <summary>
    /// Enabled or disabled the MqttClientService
    /// </summary>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Must be unique for all applications which must call to each other
    /// </summary>
    public string ApplicationKey { get; set; } = "";
    /// <summary>
    /// The name of the service for the MQTT-Client
    /// </summary>
    public string ServiceName { get; set; } = "";
    public string BrokerHost { get; set; } = "";
    public int BrokerPort { get; set; }
    public int Timeout { get; set; } = 10000;
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    /// <summary>
    /// TLS version to use: <c>"1.0"</c>, <c>"1.1"</c>, <c>"1.2"</c> or <c>"1.3"</c>. Empty or
    /// <c>"auto"</c> leaves the choice to the operating system, which negotiates the highest
    /// version both sides support. That is the default, and the better one: a fixed version locks
    /// out a newer protocol the machine already speaks and keeps an outdated one alive after the
    /// system has retired it.
    /// </summary>
    public string TlsVersion { get; set; } = "";
    public bool EncryptWithTls { get; set; } = true;
    /// <summary>
    /// SHA-256 fingerprint of the one broker certificate to trust ("pinning"). Set this for a
    /// broker with a self-signed certificate — including the broker shipped with this library,
    /// whose certificate is issued for CN=localhost and therefore never matches the host name a
    /// remote client connects to.
    /// <para>
    /// Pinning needs no certificate authority and is still safe against a man-in-the-middle,
    /// because a substituted certificate has a different fingerprint. The value may be given
    /// with colons, with spaces or as plain hex; separators are ignored.
    /// </para>
    /// <para>
    /// The broker logs the fingerprint of its certificate on startup.
    /// </para>
    /// </summary>
    public string TrustedCertificateThumbprint { get; set; } = "";
    /// <summary>
    /// Accepts any broker certificate without validation. This is the behaviour of versions
    /// before certificate validation existed, kept as a deliberate escape hatch.
    /// <para>
    /// The connection stays encrypted, but it is no longer protected against a
    /// man-in-the-middle: whoever sits in between presents their own certificate, the client
    /// accepts it, and the broker credentials and all payloads are readable in clear text.
    /// Prefer <see cref="TrustedCertificateThumbprint"/>.
    /// </para>
    /// </summary>
    public bool AllowUntrustedCertificates { get; set; } = false;
    public DiscoverySettings Discovery { get; set; } = new();

}
public class DiscoverySettings
{
    public bool SearchForDiscoveryServer { get; set; } = false;
    public int Port { get; set; } = 5005;
    /// <summary>
    /// Timeout for discovery response in seconds.
    /// </summary>
    public int ResponseTimeoutSeconds { get; set; } = 2;
}