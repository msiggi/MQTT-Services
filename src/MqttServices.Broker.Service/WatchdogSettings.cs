namespace MqttServices.Broker.Service;

/// <summary>
/// Settings for <see cref="BrokerWatchdog"/>.
/// </summary>
public class WatchdogSettings
{
    /// <summary>
    /// Whether the watchdog runs at all. Switch it off to debug a broker that refuses to bind
    /// without the service shutting down under you.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// How long to wait after startup before the first check. It has to outlast the library's own
    /// bind retries, which are ten attempts three seconds apart.
    /// </summary>
    public int StartupGraceSeconds { get; set; } = 45;

    /// <summary>
    /// How often to check afterwards.
    /// </summary>
    public int IntervalSeconds { get; set; } = 60;

    /// <summary>
    /// How many checks in a row have to come back without a listener before the service stops.
    /// </summary>
    public int FailuresBeforeStop { get; set; } = 2;
}
