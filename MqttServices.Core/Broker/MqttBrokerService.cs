using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Diagnostics;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace MqttServices.Core.Broker;


public class MqttBrokerService : IDisposable, IMqttBrokerService
{
    /// <summary>
    /// The service name.
    /// </summary>
    private readonly ILogger<MqttBrokerService> logger;

    /// <summary>
    /// Gets or sets the MQTT service configuration.
    /// </summary>
    public MqttBrokerSettings mqttBrokerSettings;
    private MqttServer? mqttServer;

    /// <summary>
    /// The bytes divider. (Used to convert from bytes to kilobytes and so on).
    /// </summary>
    private static double BytesDivider => 1048576.0;

    /// <summary>
    /// Initializes a new instance of the <see cref="MqttBrokerService"/> class.
    /// </summary>
    /// <param name="mqttServiceConfiguration">The MQTT service configuration.</param>
    /// <param name="serviceName">The service name.</param>
    public MqttBrokerService(ILogger<MqttBrokerService> logger, IOptions<MqttBrokerSettings> mqttBrokerSettings)
    {
        this.mqttBrokerSettings = mqttBrokerSettings.Value;
        this.logger = logger;
        // Do not start the broker in the constructor. A hosted service will start it after the host is ready.
    }

    private SslProtocols TlsVersion
    {
        get
        {
            return this.mqttBrokerSettings.TlsVersion switch
            {
                "1.0" => SslProtocols.Tls,
                "1.1" => SslProtocols.Tls11,
                "1.2" => SslProtocols.Tls12,
                "1.3" => SslProtocols.Tls13,
                _ => SslProtocols.Tls12
            };
        }
    }

    public async Task StartBroker()
    {
        if (mqttBrokerSettings.EnableBroker)
        {
            if (!this.mqttBrokerSettings.IsValid())
            {
                throw new Exception("The configuration is invalid");
            }

            logger?.LogInformation("Starting MqttBrokerService");
            await StartMqttServer();
            logger?.LogInformation("MqttBrokerService started");
        }
        else
        {
            logger?.LogInformation("MqttBrokerService is disabled by configuration.");
        }
    }

    /// <summary>
    /// Starts the MQTT server.
    /// </summary>
    private async Task StartMqttServer()
    {
        var certificate = CreateSelfSignedCertificate("localhost", "1.3.6.1.5.5.7.3.1");
        // Kopieren Sie das Zertifikat in den Maschinen-Speicher

        var optionsBuilder = new MqttServerOptionsBuilder()
            //.WithDefaultEndpoint()
            //.WithDefaultEndpointPort(this.mqttBrokerSettings.Port)
            .WithEncryptedEndpoint()
            .WithEncryptedEndpointPort(this.mqttBrokerSettings.TlsPort)
            .WithEncryptionCertificate(certificate)
            .WithRemoteCertificateValidationCallback((obj, cert, chain, ssl) => { return true; })
            .WithEncryptionSslProtocol(TlsVersion);

        if (this.mqttBrokerSettings.Port.HasValue)
        {
            optionsBuilder.WithDefaultEndpoint()
                .WithDefaultEndpointPort(this.mqttBrokerSettings.Port.Value);
        }

        var serverOptions = optionsBuilder.Build();

        const int maxRetries = 10;
        const int retryDelayMs = 3000;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            mqttServer = new MqttServerFactory(new ConsoleLogger()).CreateMqttServer(serverOptions);
            mqttServer.ValidatingConnectionAsync += this.ValidateConnectionAsync;
            mqttServer.InterceptingSubscriptionAsync += this.InterceptSubscriptionAsync;
            mqttServer.InterceptingPublishAsync += this.InterceptApplicationMessagePublishAsync;

            try
            {
                await mqttServer.StartAsync();
                logger?.LogInformation("MQTT Broker started!");
                return;
            }
            catch (Exception ex) when (IsPortInUse(ex))
            {
                if (attempt < maxRetries)
                {
                    logger?.LogWarning("MQTT-Server port already in use, retry {Attempt}/{MaxRetries} in {Delay}ms...", attempt, maxRetries, retryDelayMs);
                    await Task.Delay(retryDelayMs);
                }
                else
                {
                    logger?.LogError(ex, "Error starting MQTT-Server after {MaxRetries} retries", maxRetries);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error starting MQTT-Server");
                return;
            }
        }
    }

    private static bool IsPortInUse(Exception ex)
    {
        if (ex is SocketException se)
            return se.SocketErrorCode == SocketError.AddressAlreadyInUse;
        if (ex.InnerException is SocketException inner)
            return inner.SocketErrorCode == SocketError.AddressAlreadyInUse;
        return false;
    }

    /// <summary>
    /// Validates the MQTT connection.
    /// </summary>
    /// <param name="args">The arguments.</param>
    private Task ValidateConnectionAsync(ValidatingConnectionEventArgs args)
    {
        try
        {
            var currentUser = this.mqttBrokerSettings.Users.FirstOrDefault(u => u.UserName == args.UserName);

            if (currentUser == null)
            {
                args.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                this.LogMessage(args, true);
                return Task.CompletedTask;
            }

            if (args.UserName != currentUser.UserName)
            {
                args.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                this.LogMessage(args, true);
                return Task.CompletedTask;
            }

            if (args.Password != currentUser.Password)
            {
                args.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                this.LogMessage(args, true);
                return Task.CompletedTask;
            }

            args.ReasonCode = MqttConnectReasonCode.Success;
            this.LogMessage(args, false);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger?.LogError("An error occurred: {Exception}.", ex);
            return Task.FromException(ex);
        }
    }

    /// <summary>
    /// Validates the MQTT subscriptions.
    /// </summary>
    /// <param name="args">The arguments.</param>
    private Task InterceptSubscriptionAsync(InterceptingSubscriptionEventArgs args)
    {
        try
        {
            args.ProcessSubscription = true;
            this.LogMessage(args, true);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger?.LogError("An error occurred: {Exception}.", ex);
            return Task.FromException(ex);
        }
    }

    /// <summary>
    /// Validates the MQTT application messages.
    /// </summary>
    /// <param name="args">The arguments.</param>
    private Task InterceptApplicationMessagePublishAsync(InterceptingPublishEventArgs args)
    {
        try
        {
            args.ProcessPublish = true;
            this.LogMessage(args);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger?.LogError("An error occurred: {Exception}.", ex);
            return Task.FromException(ex);
        }
    }

    /// <summary>
    /// Logs the heartbeat message with some memory information.
    /// </summary>
    private void LogMemoryInformation()
    {
        var totalMemory = GC.GetTotalMemory(false);
        var memoryInfo = GC.GetGCMemoryInfo();
        var divider = BytesDivider;
        logger?.LogInformation(
             "Heartbeat for service {ServiceName}: Total {Total}, heap size: {HeapSize}, memory load: {MemoryLoad}.", "",
             $"{(totalMemory / divider):N3}", $"{(memoryInfo.HeapSizeBytes / divider):N3}", $"{(memoryInfo.MemoryLoadBytes / divider):N3}");
    }
    /// <summary> 
    ///     Logs the message from the MQTT subscription interceptor context. 
    /// </summary> 
    /// <param name="args">The arguments.</param> 
    /// <param name="successful">A <see cref="bool"/> value indicating whether the subscription was successful or not.</param> 
    private void LogMessage(InterceptingSubscriptionEventArgs args, bool successful)
    {
        logger?.LogInformation(
            successful
                ? "New subscription: ClientId = {ClientId}, TopicFilter = {TopicFilter}"
                : "Subscription failed for clientId = {ClientId}, TopicFilter = {TopicFilter}",
            args.ClientId,
            args.TopicFilter);
    }

    /// <summary>
    ///     Logs the message from the MQTT message interceptor context.
    /// </summary>
    /// <param name="args">The arguments.</param>
    private void LogMessage(InterceptingPublishEventArgs args)
    {
        var payload = args.ApplicationMessage?.Payload == null ? null : Encoding.UTF8.GetString(args.ApplicationMessage.Payload);

        logger?.LogInformation(
            "Message: ClientId = {ClientId}, Topic = {Topic}, Payload = {Payload}, QoS = {Qos}, Retain-Flag = {RetainFlag}",
            args.ClientId,
            args.ApplicationMessage?.Topic,
            payload,
            args.ApplicationMessage?.QualityOfServiceLevel,
            args.ApplicationMessage?.Retain);
    }

    /// <summary> 
    ///     Logs the message from the MQTT connection validation context. 
    /// </summary> 
    /// <param name="args">The arguments.</param> 
    /// <param name="showPassword">A <see cref="bool"/> value indicating whether the password is written to the log or not.</param> 
    private void LogMessage(ValidatingConnectionEventArgs args, bool showPassword)
    {
        if (showPassword)
        {
            logger?.LogInformation(
                "New connection: ClientId = {ClientId}, Endpoint = {Endpoint}, Username = {UserName}, Password = {Password}, CleanSession = {CleanSession}",
                args.ClientId,
                args.Endpoint,
                args.UserName,
                args.Password,
                args.CleanSession);
        }
        else
        {
            logger?.LogInformation(
                "New connection: ClientId = {ClientId}, Endpoint = {Endpoint}, Username = {UserName}, CleanSession = {CleanSession}",
                args.ClientId,
                args.Endpoint,
                args.UserName,
                args.CleanSession);
        }
    }

    public static X509Certificate2 CreateSelfSignedCertificate__(string subjectName, string oid)
    {
        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddIpAddress(IPAddress.Loopback);
        sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
        sanBuilder.AddDnsName("localhost");

        using (var rsa = RSA.Create(2048))
        {
            var certRequest = new CertificateRequest(
                $"CN={subjectName}",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            certRequest.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyAgreement,
                    false));

            certRequest.CertificateExtensions.Add(
                new X509EnhancedKeyUsageExtension(
                    new OidCollection { new Oid(oid) },
                    false));

            certRequest.CertificateExtensions.Add(sanBuilder.Build());

            var certificate = certRequest.CreateSelfSigned(
                DateTimeOffset.Now.AddDays(-1),
                DateTimeOffset.Now.AddYears(1));

            // Überprüfen, ob das Zertifikat bereits einen privaten Schlüssel hat
            if (!certificate.HasPrivateKey)
            {
                return certificate.CopyWithPrivateKey(rsa);
            }
            else
            {
                return certificate;
            }
        }
    }

    static X509Certificate2 CreateSelfSignedCertificate(string subject, string oid)
    {
        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddIpAddress(IPAddress.Loopback);
        sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
        sanBuilder.AddDnsName("localhost");

        using (var rsa = RSA.Create())
        {
            var certRequest = new CertificateRequest($"CN={subject}", rsa, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);

            certRequest.CertificateExtensions.Add(
                new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));

            certRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new(oid) }, false));

            certRequest.CertificateExtensions.Add(sanBuilder.Build());

            using (var certificate = certRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(10)))
            {
                var pfxCertificate = new X509Certificate2(
                    certificate.Export(X509ContentType.Pfx),
                    (string)null!,
                    X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable);

                return pfxCertificate;
            }
        }
    }


    void IDisposable.Dispose()
    {
        mqttServer?.StopAsync().GetAwaiter().GetResult();
    }
}

class ConsoleLogger : IMqttNetLogger
{
    readonly object _consoleSyncRoot = new();

    public bool IsEnabled => true;

    public void Publish(MqttNetLogLevel logLevel, string source, string message, object[]? parameters, Exception? exception)
    {
        var foregroundColor = ConsoleColor.White;
        switch (logLevel)
        {
            case MqttNetLogLevel.Verbose:
                foregroundColor = ConsoleColor.White;
                break;

            case MqttNetLogLevel.Info:
                foregroundColor = ConsoleColor.Green;
                break;

            case MqttNetLogLevel.Warning:
                foregroundColor = ConsoleColor.DarkYellow;
                break;

            case MqttNetLogLevel.Error:
                foregroundColor = ConsoleColor.Red;
                break;
        }

        if (parameters?.Length > 0)
        {
            message = string.Format(message, parameters);
        }

        lock (_consoleSyncRoot)
        {
            Console.ForegroundColor = foregroundColor;
            Console.WriteLine(message);

            if (exception != null)
            {
                Console.WriteLine(exception);
            }
        }
    }
}
