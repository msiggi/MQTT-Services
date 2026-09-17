using Microsoft.Extensions.Configuration;

namespace MqttServices.Broker.Service;

/// <summary>
/// Where the broker keeps everything that has to survive a deployment: its certificate, the
/// operational configuration and the logs.
/// </summary>
public static class DataDirectory
{
    /// <summary>Configuration key, also settable as the environment variable MQTTBROKER_DataDirectory.</summary>
    public const string ConfigurationKey = "DataDirectory";

    /// <summary>
    /// Returns the configured directory, or the platform default, and makes sure it exists.
    /// </summary>
    public static string Resolve(IConfiguration configuration)
    {
        var configured = configuration[ConfigurationKey]?.Trim();

        var directory = string.IsNullOrEmpty(configured) ? Default() : Path.GetFullPath(configured);
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string Default()
    {
        if (OperatingSystem.IsWindows())
        {
            // C:\ProgramData\MqttBroker - deliberately named after the Windows service, so that
            // seeing one in the service list tells you where to find the other.
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "MqttBroker");
        }

        return "/var/lib/mqttbroker";
    }

    /// <summary>
    /// Makes a relative path absolute below the data directory. An empty path stays empty -- the
    /// library reads that as "generate the certificate in memory on every start".
    /// </summary>
    public static string Anchor(string? path, string dataDirectory)
    {
        var value = path?.Trim() ?? "";
        if (value.Length == 0 || Path.IsPathRooted(value))
        {
            return value;
        }

        return Path.Combine(dataDirectory, value);
    }
}
