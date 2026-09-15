# MQTT-Services
Helper services to simplify communication between multiple applications using MQTT.

Powered by: https://github.com/dotnet/MQTTnet

## Table of Contents
- Features
- Quickstart: Messaging (Client)
- Request/Response (Example)
- Host your own MQTT Broker
- Discovery (Client & Server)
- TLS and certificate validation
- Configuration reference (appsettings.json)
- Logging
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

## TLS and certificate validation

The client connects over TLS and validates the broker's certificate. Encryption alone only
protects against passive eavesdropping: without validation, anyone who sits in between presents
their own certificate, the client accepts it, and the broker credentials and all payloads are
readable in clear text. Validation is what makes that attack fail.

There are three tiers. Pick the one that matches your broker:

| Configuration                          | Behaviour                                          | Use for                              |
|----------------------------------------|----------------------------------------------------|--------------------------------------|
| nothing set                            | full validation: chain, host name, validity period | a public broker with a CA certificate |
| `TrustedCertificateThumbprint` set     | exactly that one certificate, nothing else         | a self-signed broker on the LAN       |
| `AllowUntrustedCertificates: true`     | accepts any certificate                            | escape hatch, deliberately chosen     |

### Full validation (default)

Set nothing. The certificate has to be issued by a trusted authority, be valid, and be issued for
the host name in `BrokerHost`.

The most common stumbling block is the host name: if you reach a broker by IP address or by an
mDNS name such as `raspberrypi.local`, validation fails even with a perfectly good certificate,
because that name is not in the certificate. The client logs this case explicitly. Connect using
the name the certificate was issued for, or pin the certificate.

### Pinning a self-signed certificate

Pinning needs no certificate authority and is still safe against a man-in-the-middle, because a
substituted certificate has a different fingerprint. This is the right tier for the broker shipped
with this library, whose certificate is issued for `CN=localhost` and therefore never matches the
host name a client on another machine connects to.

```jsonc
"MqttClientSettings": {
  "BrokerHost": "192.168.1.50",
  "BrokerPort": 8883,
  // SHA-256 fingerprint. Colons, spaces and casing are ignored.
  "TrustedCertificateThumbprint": "72C062052124451BCDEB8B84543BA18A31B71836AE6B21A2AF13DE799547C29C"
}
```

The integrated broker logs the fingerprint of its certificate on startup:

```
MQTT broker TLS certificate: subject CN=localhost, SHA-256 fingerprint 72C0620521...
```

For any other broker, read it with OpenSSL:

```bash
openssl s_client -connect your-broker:8883 </dev/null 2>/dev/null | openssl x509 -noout -fingerprint -sha256
```

> **By default the integrated broker generates a new certificate on every start**, so its
> fingerprint changes with every restart and pinned clients stop connecting. Give it a
> `Certificate.Path` — see below — before you pin it.

### Keeping the integrated broker's certificate

Point `MqttBrokerSettings.Certificate.Path` at a PFX file. The broker creates the certificate there
on its first start and loads it on every following one, so the fingerprint stays the same and a
pinned client survives restarts:

```jsonc
"MqttBrokerSettings": {
  "EnableBroker": true,
  "TlsPort": 8883,
  "Certificate": {
    "Path": "broker-certificate.pfx",
    "Password": "keep-this-somewhere-safe",
    // The name clients dial. Without it they get a name mismatch and can only pin.
    "SubjectName": "broker.internal",
    "HostNames": [ "192.168.1.50" ]
  }
}
```

The file holds a private key. On Linux and macOS the broker restricts it to its owner; on Windows
the permissions of the containing directory apply. Without a `Password` the key is stored
unprotected and the broker says so on startup.

`SubjectName` and `HostNames` decide which names the certificate is valid for — `localhost` and
the loopback addresses are always included. They do not remove the need to pin: a self-signed
certificate has no chain to validate, whatever names it carries. What they do fix is the name
mismatch, which matters if you later replace it with a certificate from a real authority.

An expired stored certificate is replaced on the next start, which changes the fingerprint; that is
why a generated one is valid for ten years by default. A file that cannot be read — wrong password,
damaged — is **not** overwritten: the broker refuses to start instead, because silently generating a
replacement would cut off every pinned client.

The fingerprint is also available in code, for a host application that wants to show it rather than
have operators read the log:

```csharp
var thumbprint = serviceProvider.GetRequiredService<IMqttBrokerService>().CertificateThumbprint;
```

### TLS version

`TlsVersion` defaults to `auto` on both the client and the broker, which leaves the choice to the
operating system: it negotiates the highest version both sides support, and it applies the
machine's own policy about which versions are still acceptable.

Prefer that over naming a version. A fixed value ages badly in both directions — it locks out a
newer protocol the machine already speaks, and it keeps an outdated one alive after the system has
retired it. Set `1.0`, `1.1`, `1.2` or `1.3` only when something on the other side forces your
hand. An unusable value is logged and falls back to `auto`; it used to become `1.2` silently.

### Escape hatch

`AllowUntrustedCertificates: true` restores the behaviour of versions before validation existed.
The connection stays encrypted but is not protected against a man-in-the-middle. The client logs a
warning on every connection attempt. Prefer pinning.

### Migrating from an earlier version

Before this change the client accepted **every** broker certificate. If your broker has a
certificate from a real certificate authority (Let's Encrypt and the like) and you connect using
the host name it was issued for, everything keeps working and there is nothing to do.

Otherwise — a self-signed broker, or one reached by IP address — the connection now fails, and the
log says why. Read the fingerprint as shown above and put it into `TrustedCertificateThumbprint`.

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
| `EncryptWithTls`              | Set to `false` to connect without TLS. Everything, credentials included, then travels in clear text.      | `true`                           |
| `TlsVersion`                  | TLS version to use: `1.0`, `1.1`, `1.2`, `1.3`. Empty or `auto` lets the operating system negotiate.      | `auto`                           |
| `TrustedCertificateThumbprint`| SHA-256 fingerprint of the one broker certificate to trust. See [TLS and certificate validation](#tls-and-certificate-validation). | `(none)`            |
| `AllowUntrustedCertificates`  | Set to `true` to accept any broker certificate. Encrypted, but not safe against a man-in-the-middle.       | `false`                          |
| `Discovery.SearchForDiscoveryServer`| Set to `true` to enable searching for a discovery server. If found, `BrokerHost` and `BrokerPort` will be overwritten at runtime. | `false`                          |
| `Discovery.Port`               | Port number for the UDP discovery client.                                                                 | `5005`                           |
| `Discovery.ResponseTimeoutSeconds` | Timeout in seconds to wait for a response from the discovery server.                                      | `5`                              |
|                                |                                                                                                           |                                  |
| `MqttBrokerSettings`         | MQTT broker-related settings                                                                               |                                  |
| `EnableBroker`                | Set to `true` to enable the integrated MQTT broker.                                                      | `false`                          |
| `Port`                        | Port number of the MQTT broker will be listening on.                                                    | `1883`                           |
| `TlsPort`                        | Port number of the MQTT broker using TLS.                                                    | `8883`                           |
|`TlsVersion`                  | TLS version to accept: `1.0`, `1.1`, `1.2`, `1.3`. Empty or `auto` lets the operating system negotiate. | `auto`                          |    
| `Certificate.Path`            | Path to a PFX file holding the broker certificate. Empty means generate one in memory on every start, which changes the fingerprint on every restart. | `(none)`   |
| `Certificate.Password`        | Password protecting that PFX file. Empty stores the private key unprotected.                              | `(none)`                         |
| `Certificate.SubjectName`     | Common name of a generated certificate.                                                                   | `localhost`                      |
| `Certificate.HostNames`       | Further host names and IP addresses a generated certificate is valid for. `localhost` and the loopback addresses are always included. | `[]`      |
| `Certificate.ValidityDays`    | How long a generated certificate stays valid.                                                             | `3650`                           |
| `Discovery.Enabled`            | Set to `true` to enable UDP discovery server.                                                             | `false`                          |
| `Discovery.Port`               | Port number for the UDP discovery server.                                                                 | `5005`                           |
| `Discovery.OpenFirewall`       | Set to `true` to automatically open the firewall for the discovery server port (Windows only).           | `false`                          |

## Logging
All components log through the standard `Microsoft.Extensions.Logging` infrastructure, so they honour the
`Logging` section of your `appsettings.json` and any provider you registered (Console, Serilog, NLog, ...).

The logger categories follow the namespaces of the library:

| Category                        | Content                                                                 |
|---------------------------------|-------------------------------------------------------------------------|
| `MqttServices.Core`             | Everything below (broker, client, discovery, messaging)                  |
| `MqttServices.Core.Broker`      | Broker startup/shutdown, connection validation, subscriptions            |
| `MqttServices.Core.Client`      | Client connect/reconnect                                                 |
| `MqttServices.Core.Discovery`   | Discovery server and client                                              |
| `MqttServices.Core.Services`    | Messaging manager (publish/subscribe, request/response)                  |
| `MqttServices.Core.MqttNet`     | Internal diagnostics of the underlying MQTTnet library                    |

`MqttServices.Core.MqttNet` is very verbose: MQTTnet reports every packet at its `Verbose` level, which is
mapped to `Trace`. It is therefore silent under the usual `Information` default and only needs to be lowered
explicitly when you want to debug the protocol itself.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "MqttServices.Core": "Information",
      "MqttServices.Core.MqttNet": "Warning"
    }
  }
}
```

To see the raw MQTT traffic, set `"MqttServices.Core.MqttNet": "Trace"` (and make sure the provider itself
is not filtering `Trace` away).

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

