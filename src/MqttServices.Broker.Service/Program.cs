using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MqttServices.Broker.Service;
using MqttServices.Core.Broker;
using Serilog;

// A Windows service starts with C:\Windows\System32 as its working directory. Without an explicit
// content root, every relative path -- appsettings.json included -- would be looked up there.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

// Everything that outlives a deployment lives here, never in the publish folder: that folder is
// overwritten on the next deployment, which would silently replace the broker's certificate and
// with it the fingerprint that clients pin.
var dataDirectory = DataDirectory.Resolve(builder.Configuration);
var logDirectory = Path.Combine(dataDirectory, "logs");
Directory.CreateDirectory(logDirectory);

// The real configuration -- credentials above all -- stays outside the repository and outside the
// publish folder. The shipped appsettings.json holds placeholders only.
builder.Configuration
    .AddJsonFile(Path.Combine(dataDirectory, "appsettings.Production.json"), optional: true, reloadOnChange: true)
    .AddEnvironmentVariables("MQTTBROKER_");

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(logDirectory, "broker-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 31)
    .CreateLogger();

// AddSerilog on the logging builder adds Serilog as one provider among others, so the Windows
// event log provider that AddWindowsService registers keeps working. That one matters: a service
// that dies during startup has no other way to tell anyone.
builder.Logging.AddSerilog(dispose: true);

builder.Services.AddWindowsService(options => options.ServiceName = "MqttBroker");

// Validate before the host is even built, so a misconfiguration fails with a readable message
// instead of somewhere inside dependency injection.
var configuredSettings = new MqttBrokerSettings();
builder.Configuration.GetSection(nameof(MqttBrokerSettings)).Bind(configuredSettings);
configuredSettings.Certificate.Path = DataDirectory.Anchor(configuredSettings.Certificate.Path, dataDirectory);
StartupValidation.Validate(configuredSettings, builder.Environment);

builder.Services.AddMqttBrokerService(settings =>
{
    builder.Configuration.GetSection(nameof(MqttBrokerSettings)).Bind(settings);

    // A relative certificate path would land in the publish folder. Anchoring it to the data
    // directory keeps the shipped configuration from causing the problem described above.
    settings.Certificate.Path = DataDirectory.Anchor(settings.Certificate.Path, dataDirectory);
});

builder.Services.Configure<WatchdogSettings>(builder.Configuration.GetSection(nameof(WatchdogSettings)));
builder.Services.AddHostedService<BrokerWatchdog>();

var host = builder.Build();

host.Services.GetRequiredService<ILoggerFactory>()
    .CreateLogger("Startup")
    .LogInformation(
        "MQTT broker service starting. Environment {Environment}, data directory {DataDirectory}, TLS port {TlsPort}, certificate {CertificatePath}.",
        builder.Environment.EnvironmentName,
        dataDirectory,
        configuredSettings.TlsPort,
        configuredSettings.Certificate.Path);

try
{
    host.Run();
}
finally
{
    Log.CloseAndFlush();
}
