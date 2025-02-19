using MqttServices.Core.Client;
using MqttServices.Core.Services;
using SampleClientMessaging1;
using SampleClientMessaging1.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<RequestWorker>();

builder.Services.AddMqttClientService(opts => builder.Configuration.GetSection(nameof(MqttClientSettings)).Bind(opts), "testapp");
//builder.Services.AddSingleton<IMessagingManager, MessagingManager>();

builder.Services.AddSingleton<ApplicationService1>();
builder.Services.AddSingleton<ApplicationService2>();

var host = builder.Build();
host.Run();
