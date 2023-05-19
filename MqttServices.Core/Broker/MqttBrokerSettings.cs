using MqttServices.Core.Common;

namespace MqttServices.Core.Broker;

public class MqttBrokerSettings
{
    public bool EnableBroker { get; set; } = false;
    /// <summary>
    ///     Gets or sets the port.
    /// </summary>
    public int Port { get; set; } = 1883;

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