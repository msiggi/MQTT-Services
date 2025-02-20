using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MqttServices.Core.Client;

public class MqttClientService : IDisposable, IMqttClientService
{
    private readonly MqttClientSettings mqttClientSettings;
    private readonly ILogger<MqttClientService> logger;
    private MqttClientFactory mqttClientFactory;
    private IMqttClient mqttClient;

    public event EventHandler<MqttClientConnectedEventArgs>? ClientConnected;
    //public event EventHandler<ConnectingFailedEventArgs>? ClientConnectionFailed;
    public event EventHandler<MqttApplicationMessageReceivedEventArgs>? MessageReceived;
    public bool IsConnected { get; set; }

    private SslProtocols TlsVersion
    {
        get
        {
            return this.mqttClientSettings.TlsVersion switch
            {
                "1.0" => SslProtocols.Tls,
                "1.1" => SslProtocols.Tls11,
                "1.2" => SslProtocols.Tls12,
                "1.3" => SslProtocols.Tls13,
                _ => SslProtocols.Tls12
            };
        }
    }

    public MqttClientService(ILogger<MqttClientService> logger, IOptions<MqttClientSettings> mqttClientSettings)
    {
        IsConnected = false;
        this.mqttClientSettings = mqttClientSettings.Value;
        this.logger = logger;

        if (string.IsNullOrEmpty(this.mqttClientSettings.BrokerHost))
        {
            this.logger?.LogWarning("Invalid Broker-Settings (no Host configured)");
            return;
        }

        mqttClientFactory = new MqttClientFactory();
        mqttClient = mqttClientFactory.CreateMqttClient();
        mqttClient.ConnectedAsync += MqttClient_ConnectedAsync;
        // mqttClient.ConnectingFailedAsync += MqttClient_ConnectingFailedAsync;
        mqttClient.ApplicationMessageReceivedAsync += MqttClient_ApplicationMessageReceivedAsync;

        //if (this.mqttClientSettings.Enabled)
        //{
        //    Connect().Wait();
        //}
        //else
        //{
        //    logger?.LogInformation("MQTT-Client is disabled per configuration");
        //}
    }

    private async Task MqttClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        MessageReceived?.Invoke(this, arg);
        await Task.CompletedTask;
    }

    public async Task Connect()
    {
        if (!mqttClient.IsConnected)
        {
            try
            {
                var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(mqttClientSettings.BrokerHost, mqttClientSettings.BrokerPort)
                      .WithTlsOptions(
                      o =>
                      {
                          // The used public broker sometimes has invalid certificates. This sample accepts all
                          // certificates. This should not be used in live environments.
                          o.WithCertificateValidationHandler(_ => true);

                          // The default value is determined by the OS. Set manually to force version.
                          o.WithSslProtocols(SslProtocols.Tls12);
                      })
                      .WithCredentials(mqttClientSettings.UserName, mqttClientSettings.Password)
                        .WithClientId(mqttClientSettings.ServiceName + Guid.NewGuid().ToString())
                  .Build();

                using var timeout = new CancellationTokenSource(mqttClientOptions.Timeout);
                MqttClientConnectResult result = await mqttClient.ConnectAsync(mqttClientOptions, timeout.Token);
                if (result.ResultCode == MqttClientConnectResultCode.Success)
                {
                    logger?.LogInformation($"MQTT-Client connected to {mqttClientSettings.BrokerHost}:{mqttClientSettings.BrokerPort}");
                }
                else
                {
                    logger?.LogError($"MQTT-Client connection failed to {mqttClientSettings.BrokerHost}:{mqttClientSettings.BrokerPort}, Result-Code: {result.ResultCode.ToString()}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error connecting to MQTT Broker");
                Thread.Sleep(2000);
                await Connect();
            }
        }
    }
    public async Task Disconnect()
    {
        var mqttClientDisconnectOptions = mqttClientFactory.CreateClientDisconnectOptionsBuilder().Build();
        await mqttClient.DisconnectAsync();
    }

    public async Task PublishMessage(string topic, object payload)
    {
        if (mqttClient.IsConnected)
        {
            try
            {
                var serializeOptions = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                if (mqttClientSettings.SerializeWithCamelCase)
                    serializeOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

                if (mqttClientSettings.IgnoreCycles)
                    serializeOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;

                var applicationMessage = new MqttApplicationMessageBuilder()
                       .WithTopic(topic)
                       .WithPayload(JsonSerializer.Serialize(payload, serializeOptions))
                       .Build();

                await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error PublishMessage");
            }
        }
    }
    public async Task PublishMessage(string topic, string payload)
    {
        try
        {
            if (mqttClient.IsConnected)
            {
                var applicationMessage = new MqttApplicationMessageBuilder()
                       .WithTopic(topic)
                       .WithPayload(payload)
                       .Build();

                await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error PublishMessage");
        }
    }

    public async Task Subscribe(string topic)
    {
        if (mqttClient.IsConnected)
        {
            logger.LogInformation($"Subscribing to {topic}");
            await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).Build());
        }
    }

    //private Task MqttClient_ConnectingFailedAsync(ConnectingFailedEventArgs arg)
    //{
    //    IsConnected = false;
    //    logger?.LogError(arg.Exception, arg.Exception.Message);
    //    ClientConnectionFailed?.Invoke(this, arg);
    //    return Task.CompletedTask;
    //}

    private Task MqttClient_ConnectedAsync(MqttClientConnectedEventArgs arg)
    {
        IsConnected = true;
        logger?.LogInformation($"MQTT Connection to {mqttClientSettings.BrokerHost}:{mqttClientSettings.BrokerPort} successful!");
        ClientConnected?.Invoke(this, arg);

        return Task.CompletedTask;
    }

    //private ManagedMqttClientOptions TcpMqttClientOptions(MqttClientSettings settings)
    //{
    //    return new ManagedMqttClientOptionsBuilder()
    //        .WithClientOptions(
    //            new MqttClientOptionsBuilder()
    //                .WithClientId(settings.ServiceName + Guid.NewGuid().ToString())
    //                .WithTcpServer($"{settings.BrokerHost}", settings.BrokerPort)
    //                .WithTls(o =>
    //                {
    //                    o.UseTls = settings.EncryptWithTls;
    //                    // The used public broker sometimes has invalid certificates. This sample accepts all
    //                    // certificates. This should not be used in live environments.
    //                    o.CertificateValidationHandler = _ => true;

    //                    // The default value is determined by the OS. Set manually to force version.
    //                    o.SslProtocol = TlsVersion;
    //                })
    //                .WithCredentials(settings.UserName, settings.Password)
    //                .WithCleanSession()
    //                .Build()
    //        )
    //        .Build();
    //}

    //private ManagedMqttClientOptions TlsMqttClientOptions(string url, string caFile)
    //{
    //    return new ManagedMqttClientOptionsBuilder()
    //        .WithClientOptions(
    //            new MqttClientOptionsBuilder()
    //                .WithClientId("EMQX_" + Guid.NewGuid().ToString())
    //                .WithTcpServer(url)
    //                //.WithCredentials("user", "pass")
    //                .WithCleanSession()
    //                .WithTls(
    //                    new MqttClientOptionsBuilderTlsParameters()
    //                    {
    //                        UseTls = true,
    //                        SslProtocol = TlsVersion,
    //                        Certificates = new List<X509Certificate>()
    //                        {
    //                            // Download from https://www.emqx.com/en/mqtt/public-mqtt5-broker
    //                            X509Certificate.CreateFromCertFile(caFile)
    //                        }
    //                    }
    //                )
    //                .Build()
    //        )
    //        .Build();
    //}

    public async void Dispose()
    {
        await Disconnect();
    }
}