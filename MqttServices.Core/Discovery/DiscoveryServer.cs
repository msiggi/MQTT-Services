using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MqttServices.Core.Broker;
using MqttServices.Core.Client;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace MqttServices.Core.Discovery;

public class DiscoveryServer
{
    private readonly ILogger<DiscoveryServer> logger;

    public DiscoveryServer(ILogger<DiscoveryServer> logger)
    {
        this.logger = logger;
    }

    public async Task StartServer(int discoveryPort, int brokerPort, CancellationToken cancellationToken)
    {
        try
        {
            using var udp = new UdpClient(discoveryPort);
            // Optionally allow quick restarts in dev
            udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            logger?.LogInformation("[DiscoveryServer] Lausche auf UDP-Port {Port} ...", discoveryPort);

            while (!cancellationToken.IsCancellationRequested)
            {
                UdpReceiveResult result;
                try
                {
                    result = await udp.ReceiveAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                var received = Encoding.UTF8.GetString(result.Buffer);
                logger?.LogInformation("[DiscoveryServer] Anfrage von {Endpoint}: {Msg}", result.RemoteEndPoint, received);

                if (received == "DISCOVER_MQTT_CONFIG")
                {
                    MqttClientSettings mqttClientSettings = new MqttClientSettings
                    {
                        BrokerHost = Dns.GetHostName(),
                        BrokerPort = brokerPort
                    };

                    // Serialize the object to JSON
                    var responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(mqttClientSettings));
                    await udp.SendAsync(responseBytes, responseBytes.Length, result.RemoteEndPoint);
                    logger?.LogInformation("[DiscoveryServer] Antwort an {Endpoint}: {Host}:{Port}", result.RemoteEndPoint, mqttClientSettings.BrokerHost, mqttClientSettings.BrokerPort);
                }
            }
        }
        catch (SocketException ex)
        {
            logger?.LogError(ex, "[DiscoveryServer] SocketException beim Binden an Port {Port}", discoveryPort);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "[DiscoveryServer] Unerwarteter Fehler");
        }
        finally
        {
            logger?.LogInformation("[DiscoveryServer] Stop.");
        }
    }
}
