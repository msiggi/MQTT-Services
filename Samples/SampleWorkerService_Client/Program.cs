using MqttServices.Core.Client;
using MqttServices.Core.Common;
using SampleWorkerService_Client;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // for MQTT-Client
        services.AddMqttMessagingService(opts => context.Configuration.GetSection(nameof(MqttClientSettings)).Bind(opts));

        //services.AddHostedService<Worker>();
        services.AddHostedService<WorkerHostedService>();
    })
    .Build();

host.Run();
