using MqttServices.Core.Client;
using MqttServices.Core.Services;
using SampleClientMessaging1;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<RequestWorker>();

builder.Services.AddMqttClientService(opts => builder.Configuration.GetSection(nameof(MqttClientSettings)).Bind(opts));
builder.Services.AddSingleton<IMessagingManager, MessagingManager>();

var host = builder.Build();
host.Run();
