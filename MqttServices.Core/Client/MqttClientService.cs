using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace MqttServices.Core.Client;

public class MqttClientService : IDisposable, IMqttClientService
{
    private readonly MqttClientSettings mqttClientSettings;
    private readonly ILogger<MqttClientService> logger;
    private IManagedMqttClient mqttClient;

    public event EventHandler<MqttClientConnectedEventArgs>? ClientConnected;
    public event EventHandler<ConnectingFailedEventArgs>? ClientConnectionFailed;
    public event EventHandler<MqttApplicationMessageReceivedEventArgs>? MessageReceived;
    public bool IsConnected { get; set; }

    public MqttClientService(ILogger<MqttClientService> logger, IOptions<MqttClientSettings> mqttClientSettings)
    {
        IsConnected = false;
        this.mqttClientSettings = mqttClientSettings.Value;
        this.logger = logger;

        if (string.IsNullOrEmpty(this.mqttClientSettings.BrokerHost))
        {
            this.logger.LogWarning("Invalid Broker-Settings (no Host configured)");
            return;
        }

        mqttClient = new MqttFactory().CreateManagedMqttClient();
        mqttClient.ConnectedAsync += MqttClient_ConnectedAsync;
        mqttClient.ConnectingFailedAsync += MqttClient_ConnectingFailedAsync;
        mqttClient.ApplicationMessageReceivedAsync += MqttClient_ApplicationMessageReceivedAsync;

        if (this.mqttClientSettings.Enabled)
        {
            Connect().Wait();
        }
        else
        {
            logger.LogInformation("MQTT-Client is disabled per configuration");
        }
    }
    //public async Task StartAsync(CancellationToken cancellationToken)
    //{
    //    await Connect();
    //}

    //public async Task StopAsync(CancellationToken cancellationToken)
    //{
    //    await mqttClient.StopAsync();
    //}

    private async Task MqttClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        MessageReceived?.Invoke(this, arg);
        await Task.CompletedTask;
    }

    public async Task Connect()
    {
        var mqttClientOptions = TcpMqttClientOptions(this.mqttClientSettings);
        await mqttClient.StartAsync(mqttClientOptions);
    }

    public async Task PublishMessage(string topic, object payload)
    {
        if (mqttClient.IsConnected)
        {
            var serializeCamelCase = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var applicationMessage = new MqttApplicationMessageBuilder()
                   .WithTopic(topic)
                   .WithPayload(mqttClientSettings.SerializeWithCamelCase ? JsonSerializer.Serialize(payload, serializeCamelCase) : JsonSerializer.Serialize(payload))
                   .Build();

            var result = await mqttClient.InternalClient.PublishAsync(applicationMessage, CancellationToken.None);
        }
    }

    public async Task Subscribe(string topic)
    {
        if (mqttClient.IsConnected)
        {
            await mqttClient.InternalClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).Build());
        }
    }

    private Task MqttClient_ConnectingFailedAsync(ConnectingFailedEventArgs arg)
    {
        IsConnected = false;
        logger.LogError(arg.Exception, arg.Exception.Message);
        ClientConnectionFailed?.Invoke(this, arg);
        return Task.CompletedTask;
    }

    private Task MqttClient_ConnectedAsync(MqttClientConnectedEventArgs arg)
    {
        IsConnected = true;
        logger.LogInformation($"MQTT Connection successful!");
        ClientConnected?.Invoke(this, arg);

        return Task.CompletedTask;
    }

    private ManagedMqttClientOptions TcpMqttClientOptions(MqttClientSettings settings)
    {
        return new ManagedMqttClientOptionsBuilder()
            .WithClientOptions(
                new MqttClientOptionsBuilder()
                    .WithClientId(settings.ServiceName + Guid.NewGuid().ToString())
                    .WithTcpServer($"{settings.BrokerHost}", settings.BrokerPort)
                    .WithTls(o =>
                    {
                        o.UseTls = true;
                        // The used public broker sometimes has invalid certificates. This sample accepts all
                        // certificates. This should not be used in live environments.
                        o.CertificateValidationHandler = _ => true;

                        // The default value is determined by the OS. Set manually to force version.
                        o.SslProtocol = SslProtocols.Tls12;
                    })
                    .WithCredentials(settings.UserName, settings.Password)
                    .WithCleanSession()
                    .Build()
            )
            .Build();
    }

    private ManagedMqttClientOptions TlsMqttClientOptions(string url, string caFile)
    {
        return new ManagedMqttClientOptionsBuilder()
            .WithClientOptions(
                new MqttClientOptionsBuilder()
                    .WithClientId("EMQX_" + Guid.NewGuid().ToString())
                    .WithTcpServer(url)
                    //.WithCredentials("user", "pass")
                    .WithCleanSession()
                    .WithTls(
                        new MqttClientOptionsBuilderTlsParameters()
                        {
                            UseTls = true,
                            SslProtocol = System.Security.Authentication.SslProtocols.Tls12,
                            Certificates = new List<X509Certificate>()
                            {
                                // Download from https://www.emqx.com/en/mqtt/public-mqtt5-broker
                                X509Certificate.CreateFromCertFile(caFile)
                            }
                        }
                    )
                    .Build()
            )
            .Build();
    }

    public void Dispose()
    {
        mqttClient.StopAsync();
    }
}