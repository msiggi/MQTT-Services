using MqttServices.Core.Client;
using SampleCommon;
using SampleRemoteResponder;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddMqttClientService(opts => builder.Configuration.GetSection(nameof(MqttClientSettings)).Bind(opts));
builder.Services.AddRemoteCallResponder<GuitarPlayer>("topicPrefix");

var host = builder.Build();
host.Run();
