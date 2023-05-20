# MQTT-Services
Helper-Services for easier implementing MQTT-Broker and Clients 

using https://github.com/dotnet/MQTTnet

## Usage

install Nuget-Package https://www.nuget.org/packages/MQTT-Services

```
dotnet add package MQTT-Services --version 1.0.4
```

Add in Startup.cs/Program.cs:

```csharp
IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // for Broker
        services.AddMqttBrokerService(opts => context.Configuration.GetSection(nameof(MqttBrokerSettings)).Bind(opts));

        // for MQTT-Client
        services.AddMqttClientService(opts => context.Configuration.GetSection(nameof(MqttClientSettings)).Bind(opts));
```
