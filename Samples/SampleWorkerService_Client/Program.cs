using MqttServices.Core.Client;
using MqttServices.Core.Common;
using SampleWorkerService_Client;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // for MQTT-Client
        services.AddMqttClientService(opts => context.Configuration.GetSection(nameof(MqttClientSettings)).Bind(opts));
        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();
