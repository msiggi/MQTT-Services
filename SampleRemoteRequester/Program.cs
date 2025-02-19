using MqttServices.Core.Client;
using MqttServices.Core.Services;
using SampleCommon;
using SampleRemoteRequester;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddMqttClientService(opts => builder.Configuration.GetSection(nameof(MqttClientSettings)).Bind(opts));

//builder.Services.AddRemoteCallRequester<GuitarPlayer>("topicPrefix");

var host = builder.Build();
host.Run();
