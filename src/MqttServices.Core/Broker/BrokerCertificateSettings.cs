namespace MqttServices.Core.Broker;

/// <summary>
/// Where the broker's TLS certificate comes from.
/// </summary>
public class BrokerCertificateSettings
{
    /// <summary>
    /// Path to a PFX file holding the certificate and its private key.
    /// <para>
    /// Left empty — the default — the broker generates a certificate in memory on every start.
    /// Its fingerprint then changes with every restart, so clients cannot pin it
    /// (<c>MqttClientSettings.TrustedCertificateThumbprint</c>) beyond a single broker run.
    /// </para>
    /// <para>
    /// Set to a path, the broker loads the certificate from there, and creates and stores one on
    /// the first start if the file does not exist yet. The fingerprint then survives restarts.
    /// </para>
    /// </summary>
    public string Path { get; set; } = "";

    /// <summary>
    /// Password protecting the PFX file. An empty password stores the private key unprotected and
    /// leaves it to the file system to keep it away from other users; the broker logs a warning
    /// when it writes such a file.
    /// </summary>
    public string Password { get; set; } = "";

    /// <summary>
    /// Common name of a generated certificate. Only used when the broker creates the certificate
    /// itself.
    /// </summary>
    public string SubjectName { get; set; } = "localhost";

    /// <summary>
    /// Further host names and IP addresses the generated certificate should be valid for.
    /// <para>
    /// <c>localhost</c> and the loopback addresses are always included. Add the name or address
    /// clients actually use — a certificate that does not name it fails validation for them, and
    /// they would have to fall back to pinning.
    /// </para>
    /// </summary>
    public List<string> HostNames { get; set; } = new();

    /// <summary>
    /// How long a generated certificate stays valid. An expired stored certificate is replaced on
    /// the next start, which changes its fingerprint — so this is deliberately long.
    /// </summary>
    public int ValidityDays { get; set; } = 3650;
}
