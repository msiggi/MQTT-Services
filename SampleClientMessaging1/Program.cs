using MqttServices.Core.Client;
using MqttServices.Core.Services;
using SampleClientMessaging1;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddSingleton<IMessagingManager, MessagingManager>();
builder.Services.AddMqttClientService(opts => builder.Configuration.GetSection(nameof(MqttClientSettings)).Bind(opts));

var host = builder.Build();
host.Run();
