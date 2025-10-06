# MQTT-Services
Helper services to simplify communication between multiple applications using MQTT.

Powered by: https://github.com/dotnet/MQTTnet

## Table of Contents
- Features
- Quickstart: Messaging (Client)
- Request/Response (Example)
- Host your own MQTT Broker
- Discovery (Client & Server)
- Configuration reference (appsettings.json)
- Samples
- Contribution
- Notes

---

## Features
- Simple, generic message publishing/subscribing between applications
- Multiple overloads for sending messages
- Request/Response pattern
  - Correlate response to request via automatically generated `MessageId`
  - Predefined request types: `Generic, GetOne, GetAll, Upsert, Update, Insert, Delete`
- Optional, integrated MQTT Broker
- UDP-based discovery:
  - Discovery Server (answers broadcasts with broker info)
  - Discovery Client (finds a broker automatically and reconnects)

---

## Quickstart: Messaging (Client)

### Install
```
Install-Package MQTT-Services
```
### Register
In `Program.cs` (or `Startup.cs`):
```csharp
builder.Services.AddMqttMessagingService(opts => builder.Configuration.GetSection(nameof(MqttClientSettings)).Bind(opts));
```

Set client connection parameters in `appsettings.json`:

```json
"MqttClientSettings": {
    "ApplicationKey": "mqtttestapp",  // must be unique for all communicating applications
    "ServiceName": "MyApp",
    "BrokerHost": "localhost",
    "BrokerPort": 8883,
    "UserName": "Testuser",
    "Password": "SecRet"
  }
```
Important:
- All apps must connect to the same MQTT broker and use the same `ApplicationKey`.
- If `Discovery.SearchForDiscoveryServer = true`, `BrokerHost`/`BrokerPort` will be overwritten at runtime upon successful discovery, and an automatic reconnect will occur.

### Send a Simple Message
Inject `IMessagingManager` into your class:
```csharp
public MyClass(IMessagingManager messagingManager)
{
    this.messagingManager = messagingManager;
    this.messagingManager.ResponseReceived += MessagingManager_ResponseReceived;
}
```

Send a message with a routing key (Exchange Name):
```csharp
// Sender app:
await messagingManager.SendMessage<GuitarPlayer>(guitarPlayer1, "guitarplayersexchangename");
```

Receive the message in the receiver app:
```csharp
messagingManager.MessageReceived += async (sender, e) =>
{
    if (e.ExchangeName == "guitarplayersexchangename")
    {
        GuitarPlayer player = (GuitarPlayer)e.Value;
        Console.WriteLine($"**** Message received: {player.Name}!");
    }
};
```

### Send Request and Wait for Response
#### Requesting App:
```csharp
// Register event for response:
messagingManager.ResponseReceived += (sender, e) =>
{
    if (e.ExchangeName == typeof(GuitarPlayer).Name)
    {
        GuitarPlayer player = (GuitarPlayer)e.Value;
        Console.WriteLine($"**** Response received: {player.Name} has a {player.OwnedGuitars.FirstOrDefault().Color} {player.OwnedGuitars.FirstOrDefault().Brand} {player.OwnedGuitars.FirstOrDefault().Model} (Default Exchange Name)!");

        if (e.MessageId != Guid.Empty)
        {
            Console.WriteLine($"MessageId: {e.MessageId}. Use this to correlate response to request");
        }   
    }
};

// Send request:
await messagingManager.SendMessageRequest<GuitarPlayer>(guitarPlayer1); // no explicit exchange name needed
```
#### Responding App:
```csharp
messagingManager.RequestReceived += async (sender, e) =>
{
    if (e.ExchangeName == typeof(GuitarPlayer).Name)
        {
            var player = (GuitarPlayer)e.Value;
            Console.WriteLine($"**** Message received: {player.Name}, adding guitar model and sending it back!");

            player.OwnedGuitars = new List<Guitar> { new Guitar { Model = "Stratocaster", Brand = "Fender", Color = "Sunburst" } };
            e.SetValue(player);

            await messagingManager.SendMessageResponse(e);
        }
};
```

## Host your own MQTT Broker
### Benefits
- Easy setup of an MQTT broker in an Asp.Net Core app, Worker Service or Console app
- No external broker installation required
- Ideal for testing and development

### Install
```
Install-Package MQTT-Services
```
### Register
Register the broker in `Startup.cs`/`Program.cs`:
```csharp
builder.Services.AddMqttBrokerService(opts => builder.Configuration.GetSection(nameof(MqttBrokerSettings)).Bind(opts));
```
### Configure
Set broker connection parameters in `appsettings.json`:

```json
"MqttBrokerSettings": {
  "EnableBroker": true,
  "Discovery": {
    "Enabled": true,
    "Port": 5005,
    "OpenFirewall": false
  }
}
```

That's it! A simple MQTT broker is now running in your application. For example, use [MQTT Explorer](https://github.com/thomasnordquist/MQTT-Explorer) to test the connection to your broker.

## Discovery (Client & Server)
### Benefits
- Automatic detection of MQTT brokers in the local network
- No manual configuration of broker details in client applications
- Supports UDP-based broadcast for discovery

### Setup

- Configure the discovery settings in `appsettings.json` for Server:
```json
    "Discovery": {
      "Enabled": true,
      "Port": 5005,
      "OpenFirewall": true
    }
```

- Configure the discovery settings in `appsettings.json` for Client:
```json
     "Discovery": {
      "SearchForDiscoveryServer": true,
      "Port": 5005,
      "ResponseTimeoutSeconds": 5
    }
```

### Usage
- The discovery server automatically responds to UDP broadcasts with the broker information.
- If client found the server it gets automatically the broker info and connects to it.

## Configuration reference (appsettings.json)

| Parameter                      | Description                                                                                               | Default Value                    |
|--------------------------------|-----------------------------------------------------------------------------------------------------------|----------------------------------|
| `MqttClientSettings`          | Client-related settings                                                                                    |                                  |
| `ApplicationKey`              | Must be unique for all communicating applications.                                                         | `(none)`                          |
| `ServiceName`                 | Name of the service/application.                                                                         | `(none)`                          |
| `BrokerHost`                  | Hostname or IP address of the MQTT broker.                                                               | `localhost`                      |
| `BrokerPort`                  | Port number of the MQTT broker.                                                                           | `1883`                           |
| `UserName`                    | Username for MQTT broker authentication.                                                                  | `(none)`                          |
| `Password`                    | Password for MQTT broker authentication.                                                                  | `(none)`                          |
| `Discovery.SearchForDiscoveryServer`| Set to `true` to enable searching for a discovery server. If found, `BrokerHost` and `BrokerPort` will be overwritten at runtime. | `false`                          |
| `Discovery.Port`               | Port number for the UDP discovery client.                                                                 | `5005`                           |
| `Discovery.ResponseTimeoutSeconds` | Timeout in seconds to wait for a response from the discovery server.                                      | `5`                              |
|                                |                                                                                                           |                                  |
| `MqttBrokerSettings`         | MQTT broker-related settings                                                                               |                                  |
| `EnableBroker`                | Set to `true` to enable the integrated MQTT broker.                                                      | `false`                          |
| `Port`                        | Port number of the MQTT broker will be listening on.                                                    | `1883`                           |
| `TlsPort`                        | Port number of the MQTT broker using TLS.                                                    | `8883`                           |
|`TlsVersion`                  | TLS version to use for secure connections. Supported values: `None`, `Tls`, `Tls11`, `Tls12`, `Tls13`. | `Tls12`                          |    
| `Discovery.Enabled`            | Set to `true` to enable UDP discovery server.                                                             | `false`                          |
| `Discovery.Port`               | Port number for the UDP discovery server.                                                                 | `5005`                           |
| `Discovery.OpenFirewall`       | Set to `true` to automatically open the firewall for the discovery server port (Windows only).           | `false`                          |

## Samples
The best overview can be found in the sample projects *SampleClientMessaging1* (Sender) and *SampleClientMessaging2* (Receiver and Responder) (Start configuration "Two Sample Clients" in the solution)

Additonally, there are samples for Broker inclunding Discovery Server in *SampleWebApiWithMqttBroker*.

## Contribution
Contributions are welcome! If you would like to contribute to this project, please follow these steps:

1. Fork the repository.
2. Create a new branch for your feature or bug fix.
3. Make your changes in the new branch.
4. Thoroughly test your changes.
5. Commit and push your changes to your forked repository.
6. Create a pull request to the main repository.

Please ensure that your code adheres to the project's coding standards and includes appropriate tests. Thank you for your contributions!

Notes:
- `AddMqttMessagingService` registers `IMqttClientService`, `IMessagingManager`, `IDiscoveryClient`, and a background service for auto-reconnect/discovery.
- Alternatively: `AddMqttClientService` (without messaging) + optional `AddMqttClientReconnectService()`.

