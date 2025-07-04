using MqttServices.Core.Client;
using MqttServices.Core.Discovery;
using SampleClientMessaging1;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<RequestWorker>();

builder.Services.AddMqttMessagingService(opts => builder.Configuration.GetSection(nameof(MqttClientSettings)).Bind(opts));
builder.Services.AddTransient<DiscoveryClient>();

var host = builder.Build();
host.Run();
