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
    public string ApplicationKey { get; set; }

    public event EventHandler<MqttClientConnectedEventArgs>? ClientConnected;
    public event EventHandler<MqttApplicationMessageReceivedEventArgs>? MessageReceived;
    public bool IsConnected { get; set; }
    public bool IsConnecting { get; set; }

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
        this.ApplicationKey = mqttClientSettings.Value.ApplicationKey;
        this.logger = logger;

        if (string.IsNullOrEmpty(this.mqttClientSettings.BrokerHost))
        {
            this.logger?.LogWarning("Invalid Broker-Settings (no Host configured)");
            return;
        }

        mqttClientFactory = new MqttClientFactory();
        mqttClient = mqttClientFactory.CreateMqttClient();
        mqttClient.ConnectedAsync += MqttClient_ConnectedAsync;
        mqttClient.DisconnectedAsync += MqttClient_DisconnectedAsync;
        mqttClient.ApplicationMessageReceivedAsync += MqttClient_ApplicationMessageReceivedAsync;
    }

    private async Task MqttClient_DisconnectedAsync(MqttClientDisconnectedEventArgs args)
    {
        IsConnected = false;
        logger?.LogWarning($"MQTT-Client disconnected from {mqttClientSettings.BrokerHost}:{mqttClientSettings.BrokerPort}");
        await Task.Delay(2000);
        logger?.LogInformation("Reconnecting...");
        await Connect();
    }

    private async Task MqttClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        MessageReceived?.Invoke(this, arg);
        await Task.CompletedTask;
    }
    public async Task ReConnect(string brokerHost, int brokerPort, string username = "", string password = "")
    {
        await Disconnect();
        mqttClientSettings.BrokerHost = brokerHost;
        mqttClientSettings.BrokerPort = brokerPort;
        mqttClientSettings.UserName = username;
        mqttClientSettings.Password = password;
        await Connect();
    }
    public async Task Connect()
    {
        if (!mqttClient.IsConnected && !IsConnecting)
        {
            var sslVersion = TlsVersion;
            IsConnecting = true;
            try
            {
                logger?.LogInformation($"MQTT-Client connecting to {mqttClientSettings.BrokerHost}:{mqttClientSettings.BrokerPort}");

                var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(mqttClientSettings.BrokerHost, mqttClientSettings.BrokerPort)
                      .WithTlsOptions(
                      o =>
                      {
                          // The used public broker sometimes has invalid certificates. This sample accepts all
                          // certificates. This should not be used in live environments.
                          o.WithCertificateValidationHandler(_ => true);

                          // The default value is determined by the OS. Set manually to force version.
                          o.WithSslProtocols(sslVersion);
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
                logger.LogError("Error connecting to MQTT Broker");
                Thread.Sleep(5000);
                IsConnecting = false;

                await Connect();
            }
        }
    }
    public async Task Disconnect()
    {
        try
        {
            await mqttClient.DisconnectAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error disconnecting MQTT-Client");
        }
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
            else
            {
                logger.LogWarning($"MQTT-Client not connected. Cannot publish message to {topic}");
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
        IsConnecting = false;
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