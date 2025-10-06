using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MqttServices.Core.Client;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace MqttServices.Core.Discovery;

public class DiscoveryClient : IDiscoveryClient
{
    private readonly int discoveryPort;
    private readonly int responseTimeoutSeconds;
    private readonly IConfiguration configuration;
    private readonly IMqttClientService mqttClientService;
    private readonly ILogger<DiscoveryClient> logger;

    public DiscoveryClient(IConfiguration configuration, IMqttClientService mqttClientService, ILogger<DiscoveryClient> logger, IOptions<MqttClientSettings> clientOptions)
    {
        this.configuration = configuration;
        this.mqttClientService = mqttClientService;
        this.logger = logger;
        this.discoveryPort = clientOptions?.Value?.Discovery?.Port ?? 5005;
        this.responseTimeoutSeconds = Math.Max(1, clientOptions?.Value?.Discovery?.ResponseTimeoutSeconds ?? 2);
    }

    public async Task SendBroadcastDiscoveryRequest()
    {
        try
        {
            using var udp = new UdpClient();
            udp.EnableBroadcast = true;

            var discoveryMsg = Encoding.UTF8.GetBytes("DISCOVER_MQTT_CONFIG");
            var broadcastEP = new IPEndPoint(IPAddress.Broadcast, discoveryPort);

            logger.LogInformation("[Discovery] Sende Discovery-Broadcast auf Port {Port} … (Timeout: {Timeout}s)", discoveryPort, responseTimeoutSeconds);
            await udp.SendAsync(discoveryMsg, discoveryMsg.Length, broadcastEP);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(responseTimeoutSeconds));

            var receiveTask = udp.ReceiveAsync();
            var completed = await Task.WhenAny(receiveTask, Task.Delay(Timeout.Infinite, cts.Token));

            if (completed != receiveTask)
            {
                logger.LogInformation("[Discovery] Keine Discovery-Antwort erhalten (Timeout nach {Timeout}s).", responseTimeoutSeconds);
                return;
            }

            var result = receiveTask.Result; // sicher, da completed
            var servicePayload = Encoding.UTF8.GetString(result.Buffer);
            var mqttClientSettings = JsonSerializer.Deserialize<MqttClientSettings>(servicePayload);

            if (mqttClientSettings is null)
            {
                logger.LogWarning("[Discovery] Antwort konnte nicht deserialisiert werden: {Payload}", servicePayload);
                return;
            }

            logger.LogInformation("[Discovery] Gefundener Broker: {Host}:{Port}", mqttClientSettings.BrokerHost, mqttClientSettings.BrokerPort);

            // Optional Konfiguration aktualisieren (falls MemoryProvider vorhanden)
            TryUpdateSetting("MqttClientSettings:BrokerHost", mqttClientSettings.BrokerHost);
            TryUpdateSetting("MqttClientSettings:BrokerPort", mqttClientSettings.BrokerPort.ToString());
            if (!string.IsNullOrWhiteSpace(mqttClientSettings.UserName))
                TryUpdateSetting("MqttClientSettings:UserName", mqttClientSettings.UserName);
            if (!string.IsNullOrWhiteSpace(mqttClientSettings.Password))
                TryUpdateSetting("MqttClientSettings:Password", mqttClientSettings.Password);

            // Laufenden Client auf neue Settings umstellen
            await mqttClientService.ReConnect(
                mqttClientSettings.BrokerHost,
                mqttClientSettings.BrokerPort);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Discovery] Fehler bei Discovery-Anfrage");
        }
    }

    public void UpdateSetting(string key, string value)
    {
        TryUpdateSetting(key, value);
    }

    private void TryUpdateSetting(string key, string value)
    {
        try
        {
            if (configuration is IConfigurationRoot root)
            {
                var memoryProvider = root.Providers.FirstOrDefault(p => p is MemoryConfigurationProvider) as MemoryConfigurationProvider;
                if (memoryProvider != null)
                {
                    memoryProvider.Set(key, value);
                    logger.LogInformation("[Discovery] Setting aktualisiert: {Key} = {Value}", key, value);
                }
                else
                {
                    // Kein Memory-Provider vorhanden – still akzeptieren, da ReConnect Laufzeitwerte nutzt
                    logger.LogDebug("[Discovery] Kein MemoryConfigurationProvider vorhanden, Setting {Key} nicht persistent aktualisiert.", key);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "[Discovery] Konnte Setting {Key} nicht aktualisieren.", key);
        }
    }
}
