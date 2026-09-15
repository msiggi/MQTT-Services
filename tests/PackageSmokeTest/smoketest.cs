#:package MQTT-Services@4.0.0-beta.1
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MqttServices.Core.Broker;
using MqttServices.Core.Client;
using MqttServices.Core.Common;
using System.Buffers;
using System.Text;

// Exercises the published package through its public API only, the way a consumer would.
//
// This is deliberately not part of the solution and has no .csproj: the package version it
// pulls does not exist yet while that version is being built, so a project here would break
// the release build, which restores **/*.csproj.
//
// It hosts the broker and the client in one process. Both come from the package, so the test
// needs no infrastructure -- and it can check the case that matters most, a certificate that
// is valid but not the expected one, which no external broker would produce on request.
//
//   dotnet run smoketest.cs
//   ./run.ps1 -Version 4.0.0-beta.2      (to test a different version)

const int Port = 18990;
const string User = "smoketest";
const string Password = "secret";

var results = new List<(string Label, bool Ok)>();
var directory = Path.Combine(Path.GetTempPath(), "mqtt-smoketest-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
var certificatePath = Path.Combine(directory, "broker.pfx");

using var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Warning).AddProvider(new SilentProvider()));

MqttBrokerService? broker = null;
MqttBrokerService? restarted = null;
try
{
    var assembly = typeof(MqttClientService).Assembly.GetName();
    Console.WriteLine($"Testing {assembly.Name} {assembly.Version}\n");

    // --- the broker, with a certificate that outlives a restart ---------------------
    broker = NewBroker();
    await broker.StartBroker();
    await Task.Delay(600);

    var thumbprint = broker.CertificateThumbprint;
    Console.WriteLine($"Broker certificate fingerprint: {thumbprint}\n");

    Check("the fingerprint is exposed on IMqttBrokerService", !string.IsNullOrEmpty(thumbprint));
    Check("the certificate was written to disk", File.Exists(certificatePath));

    if (string.IsNullOrEmpty(thumbprint))
    {
        Report();
        return;
    }

    // --- the three validation tiers -------------------------------------------------
    Check("pinned, correct fingerprint -> connects",
          await Connects(new MqttClientSettings { TrustedCertificateThumbprint = thumbprint }));

    // A single altered digit. This is the case that proves validation actually happens.
    var altered = (thumbprint[0] == '0' ? '1' : '0') + thumbprint[1..];
    Check("pinned, one digit changed -> rejected",
          !await Connects(new MqttClientSettings { TrustedCertificateThumbprint = altered }));

    Check("full validation against a self-signed broker -> rejected",
          !await Connects(new MqttClientSettings()));

    Check("AllowUntrustedCertificates -> connects",
          await Connects(new MqttClientSettings { AllowUntrustedCertificates = true }));

    // The fingerprint may be written the way it was copied from wherever.
    var withColons = string.Join(":", Enumerable.Range(0, thumbprint.Length / 2)
                                                .Select(i => thumbprint.Substring(i * 2, 2)));
    Check("pinned, OpenSSL colon format -> connects",
          await Connects(new MqttClientSettings { TrustedCertificateThumbprint = withColons }));

    // --- a message actually travelling through the channel ---------------------------
    Check("a message survives the round trip", await RoundTrip(thumbprint));

    // --- the certificate survives a restart ------------------------------------------
    ((IDisposable)broker).Dispose();
    broker = null;
    await Task.Delay(900);

    restarted = NewBroker();
    await restarted.StartBroker();
    await Task.Delay(600);

    Check("the fingerprint is unchanged after a restart", restarted.CertificateThumbprint == thumbprint);
    Check("the unchanged pinned client reconnects",
          await Connects(new MqttClientSettings { TrustedCertificateThumbprint = thumbprint }));

    Report();
}
finally
{
    if (broker is not null) ((IDisposable)broker).Dispose();
    if (restarted is not null) ((IDisposable)restarted).Dispose();
    try { Directory.Delete(directory, recursive: true); } catch { /* a temp directory */ }
}

void Report()
{
    Console.WriteLine("\n================ RESULT ================");
    foreach (var (label, ok) in results)
    {
        Console.WriteLine($"{(ok ? "OK  " : "FAIL")} {label}");
    }

    var failed = results.Count(r => !r.Ok);
    Console.WriteLine(failed == 0 ? "\neverything passed" : $"\n{failed} CHECK(S) FAILED");
    Environment.ExitCode = failed;
}

void Check(string label, bool ok) => results.Add((label, ok));

MqttBrokerService NewBroker() => new(
    loggerFactory.CreateLogger<MqttBrokerService>(),
    Options.Create(new MqttBrokerSettings
    {
        EnableBroker = true,
        TlsPort = Port,
        Users = [new MqttUser { UserName = User, Password = Password }],
        Certificate = new BrokerCertificateSettings { Path = certificatePath, Password = "pfx-password" },
    }));

MqttClientService NewClient(MqttClientSettings settings)
{
    settings.BrokerHost = "localhost";
    settings.BrokerPort = Port;
    settings.UserName = User;
    settings.Password = Password;
    settings.ServiceName = "smoketest";
    settings.Timeout = 6000;
    return new MqttClientService(loggerFactory.CreateLogger<MqttClientService>(), Options.Create(settings));
}

async Task<bool> Connects(MqttClientSettings settings)
{
    var client = NewClient(settings);
    await client.Connect();
    var connected = client.IsConnected;
    await client.Disconnect();
    return connected;
}

async Task<bool> RoundTrip(string thumbprint)
{
    const string Topic = "smoketest/roundtrip";
    const string Message = "hello from the package";

    var received = new TaskCompletionSource<string>();
    var client = NewClient(new MqttClientSettings { TrustedCertificateThumbprint = thumbprint });
    client.MessageReceived += (_, e) =>
        received.TrySetResult(Encoding.UTF8.GetString(BuffersExtensions.ToArray(e.ApplicationMessage.Payload)));

    await client.Connect();
    if (!client.IsConnected)
    {
        return false;
    }

    await client.Subscribe(Topic);
    await Task.Delay(400);
    await client.PublishMessage(Topic, Message);

    var finished = await Task.WhenAny(received.Task, Task.Delay(5000));
    await client.Disconnect();
    return finished == received.Task && received.Task.Result == Message;
}

/// <summary>Keeps the library's own logging out of the report.</summary>
sealed class SilentProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new SilentLogger();
    public void Dispose() { }

    sealed class SilentLogger : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                                Func<TState, Exception?, string> formatter) { }
    }
}
