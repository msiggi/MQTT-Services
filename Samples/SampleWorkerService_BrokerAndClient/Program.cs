using SampleWorkerService_BrokerAndClient;
using MqttServices.Core.Common;
using MqttServices.Core.Broker;
using MqttServices.Core.Client;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // for Broker
        services.AddMqttBrokerService(opts => context.Configuration.GetSection(nameof(MqttBrokerSettings)).Bind(opts));

        // for MQTT-Client
        services.AddMqttClientService(opts => context.Configuration.GetSection(nameof(MqttClientSettings)).Bind(opts));
        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();
