using MqttServices.Core.Common;

namespace MqttServices.Core.Broker;

public class DiscoverySettings
{
    public bool Enabled { get; set; } = false;
    public bool OpenFirewall { get; set; } = false;
    public int Port { get; set; } = 5005;
}

public class MqttBrokerSettings
{
    public bool EnableBroker { get; set; } = false;
    /// <summary>
    ///     Gets or sets the port.
    /// </summary>
    public int? Port { get; set; }

    /// <summary>
    ///     Gets or sets the list of valid users.
    /// </summary>
    public List<MqttUser> Users { get; set; } = new();

    /// <summary>
    /// Gets or sets the heartbeat delay in milliseconds.
    /// </summary>
    public int DelayInMilliSeconds { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the TLS port.
    /// </summary>
    public int TlsPort { get; set; } = 8883;

    /// <summary>
    /// TLS version to accept: <c>"1.0"</c>, <c>"1.1"</c>, <c>"1.2"</c> or <c>"1.3"</c>. Empty or
    /// <c>"auto"</c> leaves the choice to the operating system, which negotiates the highest
    /// version both sides support. That is the default, and the better one: a fixed version locks
    /// out a newer protocol the machine already speaks and keeps an outdated one alive after the
    /// system has retired it.
    /// </summary>
    public string TlsVersion { get; set; } = "";

    /// <summary>
    /// Where the broker's TLS certificate comes from. By default it is generated in memory on
    /// every start; configure <see cref="BrokerCertificateSettings.Path"/> to keep it stable
    /// across restarts, which is what clients pinning the certificate need.
    /// </summary>
    public BrokerCertificateSettings Certificate { get; set; } = new();

    public DiscoverySettings Discovery { get; set; } = new();

    /// <summary>
    /// If true, attempt to open firewall rules for broker ports on supported platforms.
    /// </summary>
    public bool OpenFirewall { get; set; } = false;

    /// <summary>
    /// Checks whether the configuration is valid or not.
    /// </summary>
    /// <returns>A value indicating whether the configuration is valid or not.</returns>
    public bool IsValid()
    {
        if (this.Port is <= 0 or > 65535)
        {
            throw new Exception("The port is invalid");
        }

        if (!this.Users.Any())
        {
            throw new Exception("The users are invalid");
        }

        if (this.DelayInMilliSeconds <= 0)
        {
            throw new Exception("The heartbeat delay is invalid");
        }

        if (this.TlsPort is <= 0 or > 65535)
        {
            throw new Exception("The TLS port is invalid");
        }

        return true;
    }
}