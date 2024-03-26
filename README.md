# MQTT-Services
Helper-Services for easier implementing MQTT-Broker and Clients 

using https://github.com/dotnet/MQTTnet

## General Usage

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


For subscribe, receive and publish messages see Sample-Project *SampleWorkerService_BrokerAndClient*

## Use generic Messaging Service
### Features
- simple generic message send
- generic request/response pattern

Register Service in Startup.cs/Program.cs:
```csharp
services.AddSingleton<IMessagingManager, MessagingManager>();
```

Request Message using exchange string to identify the response:
```csharp
await messagingManager.SendMessageRequest<PersonDataRequest>(payloadPersonRequest, "personExchange");

```

check for response in ResponseReceived-Event:
```csharp
messagingManager.ResponseReceived += (sender, args) =>
{
	if (args.Exchange == "personExchange")
	{
		var response = args.Response as PersonDataResponse;
		Console.WriteLine($"Received Response: {response?.Name}");
	}
};
```

Checkout full example in Sample-Project *SampleClientMessaging1* (Sender) and *SampleClientMessaging2* (Receiver and Responder)