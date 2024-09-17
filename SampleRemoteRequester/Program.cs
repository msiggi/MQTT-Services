using MqttServices.Core.Client;
using SampleCommon;
using SampleRemoteRequester;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddMqttClientService(opts => builder.Configuration.GetSection(nameof(MqttClientSettings)).Bind(opts));
builder.Services.AddRemoteCallRequester<PersonData>("topicPrefix");

var host = builder.Build();
host.Run();
