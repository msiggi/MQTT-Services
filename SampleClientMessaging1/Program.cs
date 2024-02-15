using MqttServices.Core.Client;
using SampleClientMessaging1;
using SampleCommon;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddSingleton<IMessagingManager, MessagingManager>();
builder.Services.AddMqttClientService(opts => builder.Configuration.GetSection(nameof(MqttClientSettings)).Bind(opts));

var host = builder.Build();
host.Run();
