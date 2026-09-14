using Microsoft.Extensions.Logging;
using MQTTnet.Diagnostics.Logger;

namespace MqttServices.Core.Broker;

/// <summary>
/// Forwards the internal MQTTnet diagnostics output to the <see cref="ILogger"/> infrastructure
/// of the hosting application, so it can be filtered and routed via the usual
/// "Logging" configuration (appsettings.json, Serilog, NLog, ...).
/// </summary>
internal sealed class MqttNetLoggerAdapter : IMqttNetLogger
{
    /// <summary>
    /// The logger category used for all MQTTnet internal messages.
    /// </summary>
    public const string CategoryName = "MqttServices.Core.MqttNet";

    private readonly ILogger logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MqttNetLoggerAdapter"/> class.
    /// </summary>
    /// <param name="logger">The logger the MQTTnet messages are written to.</param>
    public MqttNetLoggerAdapter(ILogger logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc />
    public bool IsEnabled => this.logger.IsEnabled(LogLevel.Trace)
                             || this.logger.IsEnabled(LogLevel.Information)
                             || this.logger.IsEnabled(LogLevel.Warning)
                             || this.logger.IsEnabled(LogLevel.Error);

    /// <inheritdoc />
    public void Publish(MqttNetLogLevel logLevel, string source, string message, object[]? parameters, Exception? exception)
    {
        var level = Map(logLevel);

        if (!this.logger.IsEnabled(level))
        {
            return;
        }

        // MQTTnet uses positional placeholders ({0}, {1}, ...) which are not compatible
        // with the named-placeholder message templates of ILogger, so the message is
        // pre-formatted and then passed as a plain value.
        if (parameters?.Length > 0)
        {
            try
            {
                message = string.Format(message, parameters);
            }
            catch (FormatException)
            {
                // Keep the raw message if the placeholders do not match the parameters.
            }
        }

        this.logger.Log(level, exception, "[{Source}] {MqttNetMessage}", source, message);
    }

    /// <summary>
    /// Maps a <see cref="MqttNetLogLevel"/> to the corresponding <see cref="LogLevel"/>.
    /// </summary>
    private static LogLevel Map(MqttNetLogLevel logLevel) => logLevel switch
    {
        MqttNetLogLevel.Verbose => LogLevel.Trace,
        MqttNetLogLevel.Info => LogLevel.Information,
        MqttNetLogLevel.Warning => LogLevel.Warning,
        MqttNetLogLevel.Error => LogLevel.Error,
        _ => LogLevel.Debug
    };
}
