# MQTT-Services
Helper-Services for easier implementing MQTT-Broker and Clients 

using https://github.com/dotnet/MQTTnet

## Usage

install Nuget-Package https://www.nuget.org/packages/MQTT-Services

```
dotnet add package MQTT-Services
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

using following configuration for Broker in appsettings.json:
```json
  "MqttBrokerSettings": {
    "EnableBroker": true,
    "Port": 1883,
    "Users": [
      {
        "UserName": "Hans",
        "Password": "Test"
      }
    ],
    "DelayInMilliSeconds": 30000,
    "TlsPort": 8883
  }
```

... and for the client:
```json
"MqttClientSettings": {
    "ServiceName": "SampleWorkerService_BrokerAndClient",
    "BrokerHost": "localhost",
    "BrokerPort": 8883,
    "UserName": "Hans",
    "Password": "Test"
  }
```




