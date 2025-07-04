using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using MqttServices.Core.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MqttServices.Core.Discovery;

public class DiscoveryClient
{
    private const int DiscoveryPort = 5005;
    private readonly IConfiguration configuration;
    private readonly IMqttClientService mqttClientService;

    public DiscoveryClient(IConfiguration configuration, IMqttClientService mqttClientService)
    {
        this.configuration = configuration;
        this.mqttClientService = mqttClientService;
    }

    public async Task SendBroadcastDiscoveryRequest()
    {
        using var udp = new UdpClient();
        udp.EnableBroadcast = true;

        var discoveryMsg = Encoding.UTF8.GetBytes("DISCOVER_MQTT_CONFIG");
        var broadcastEP = new IPEndPoint(IPAddress.Broadcast, DiscoveryPort);

        Console.WriteLine($"[Client] Sende Discovery-Broadcast auf Port {DiscoveryPort} ...");
        udp.Send(discoveryMsg, discoveryMsg.Length, broadcastEP);

        var clientEndpoint = new IPEndPoint(IPAddress.Any, 0);
        udp.Client.ReceiveTimeout = 2000; // 2 Sekunden warten

        try
        {
            var response = udp.Receive(ref clientEndpoint);
            var serviceUrl = Encoding.UTF8.GetString(response);
            MqttClientSettings mqttClientSettings = JsonSerializer.Deserialize<MqttClientSettings>(serviceUrl);
            Console.WriteLine($"[Client] Gefundener Broker: {mqttClientSettings.BrokerHost}:{mqttClientSettings.BrokerPort}");

            // Update the in-memory configuration with the discovered settings
            UpdateSetting("MqttClientSettings:BrokerHost", mqttClientSettings.BrokerHost);
            UpdateSetting("MqttClientSettings:BrokerPort", mqttClientSettings.BrokerPort.ToString());

            await mqttClientService.Reconnect(mqttClientSettings.BrokerHost, mqttClientSettings.BrokerPort);
        }
        catch (SocketException)
        {
            Console.WriteLine("[Client] Keine Antwort erhalten.");
        }
    }
    public void UpdateSetting(string key, string value)
    {
        // Hiermit änderst du den Wert im In-Memory-Provider
        var memoryConfig = configuration as IConfigurationRoot;
        var provider = memoryConfig.Providers.First(p => p is MemoryConfigurationProvider) as MemoryConfigurationProvider;
        provider.Set(key, value);
    }
}
