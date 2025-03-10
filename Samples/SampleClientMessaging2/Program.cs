using MqttServices.Core.Client;
using MqttServices.Core.Services;
using SampleClientMessaging2;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMqttMessagingService(opts => builder.Configuration.GetSection(nameof(MqttClientSettings)).Bind(opts));
builder.Services.AddHostedService<ResponseWorker>();

var host = builder.Build();
host.Run();
