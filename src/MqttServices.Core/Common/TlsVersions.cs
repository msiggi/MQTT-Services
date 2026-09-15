using Microsoft.Extensions.Logging;
using System.Security.Authentication;

namespace MqttServices.Core.Common;

/// <summary>
/// Maps the configured TLS version onto <see cref="SslProtocols"/>, for both the client and the
/// broker.
/// </summary>
public static class TlsVersions
{
    /// <summary>
    /// The default: let the operating system negotiate the highest version both sides support.
    /// <para>
    /// <see cref="SslProtocols.None"/> is what "no explicit choice" means to the platform, and it
    /// is the better default than naming a version. A hard-coded version ages badly in both
    /// directions — it locks out a newer protocol the machine already speaks, and it keeps an
    /// outdated one alive after the operating system has retired it. Leaving the choice to the
    /// system means the machine's own policy decides.
    /// </para>
    /// </summary>
    public const SslProtocols OperatingSystemDefault = SslProtocols.None;

    /// <summary>
    /// Accepts <c>"1.0"</c>, <c>"1.1"</c>, <c>"1.2"</c> and <c>"1.3"</c>, or an empty value and
    /// <c>"auto"</c> for <see cref="OperatingSystemDefault"/>.
    /// </summary>
    /// <param name="configuredVersion">The value of the <c>TlsVersion</c> setting.</param>
    /// <param name="logger">Used to report an unusable value.</param>
    /// <param name="settingPath">Where the value came from, for the log message.</param>
    public static SslProtocols Parse(string? configuredVersion, ILogger? logger = null, string settingPath = "TlsVersion")
    {
        var value = configuredVersion?.Trim() ?? "";

        if (value.Length == 0 || string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase))
        {
            return OperatingSystemDefault;
        }

        switch (value)
        {
            case "1.0":
                return SslProtocols.Tls;
            case "1.1":
                return SslProtocols.Tls11;
            case "1.2":
                return SslProtocols.Tls12;
            case "1.3":
                return SslProtocols.Tls13;
        }

        // Previously an unknown value silently became TLS 1.2, which hid the typo and could just
        // as well hide an intended 1.3. Say so and fall back to the system default.
        logger?.LogWarning(
            "{SettingPath} has the unknown value \"{Value}\". Expected 1.0, 1.1, 1.2, 1.3 or auto. " +
            "Falling back to the operating system default.",
            settingPath,
            value);

        return OperatingSystemDefault;
    }
}
