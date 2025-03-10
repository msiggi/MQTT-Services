using MqttServices.Core.Client;
using SampleClientMessaging1;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<RequestWorker>();

builder.Services.AddMqttMessagingService(opts => builder.Configuration.GetSection(nameof(MqttClientSettings)).Bind(opts));

var host = builder.Build();
host.Run();
