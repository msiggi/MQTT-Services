# MqttServices.Broker.Service

The MQTT broker from `MqttServices.Core`, hosted on its own as a Windows service.

Running the broker inside an application means every deployment of that application takes the
broker down with it. As its own service it has its own lifetime: deploy ThingsBridge as often as
you like, everything else stays connected.

## Layout

| | |
|---|---|
| Binaries | wherever you deploy them, e.g. `C:\Services\MqttBroker` |
| Data directory | `C:\ProgramData\MqttBroker` |
| Certificate | `C:\ProgramData\MqttBroker\broker.pfx` |
| Production configuration | `C:\ProgramData\MqttBroker\appsettings.Production.json` |
| Logs | `C:\ProgramData\MqttBroker\logs\broker-<date>.log` |

Nothing that has to survive a deployment lives with the binaries. A relative
`Certificate:Path` is anchored to the data directory automatically, so it cannot end up in the
publish folder and be overwritten on the next deployment — which would change the certificate
fingerprint and lock out every client that pins it.

The data directory can be moved with `DataDirectory` in the configuration, or the environment
variable `MQTTBROKER_DataDirectory`.

## Setup

```powershell
dotnet publish -c Release -o C:\Temp\MqttBrokerPublish
.\deploy\install-service.ps1 -BinaryPath 'C:\Services\MqttBroker\MqttServices.Broker.Service.exe'
```

`install-service.ps1` creates the service, locks the data directory down to SYSTEM and the
administrators, opens the firewall port and sets a recovery policy.

Then write `C:\ProgramData\MqttBroker\appsettings.Production.json`:

```json
{
  "MqttBrokerSettings": {
    "Certificate": {
      "SubjectName": "mqtt.example.net",
      "HostNames": [ "mqtt.example.net", "192.168.1.50" ],
      "Password": "..."
    },
    "Users": [
      { "UserName": "core-service", "Password": "..." },
      { "UserName": "ui-app",       "Password": "..." }
    ]
  }
}
```

The shipped `appsettings.json` holds placeholders only. The service refuses to start while a user
still has an empty or `changeme` password, so a forgotten configuration fails loudly instead of
running with a known one.

### Where credentials come from

| | |
|---|---|
| Development (F5) | user secrets — `dotnet user-secrets set "MqttBrokerSettings:Users:0:Password" "..."` |
| Production | `appsettings.Production.json` in the data directory, or the environment |

User secrets are loaded only in the Development environment, so the service never reads them on
the server. That is also why the launch profile sets `DOTNET_ENVIRONMENT=Development`:
without it the environment is Production even when debugging, and the accounts in user
secrets are silently not loaded. The environment is in the first log line at startup. Environment variables work either way, with the prefix `MQTTBROKER_` or without one:
`MQTTBROKER_MqttBrokerSettings__Certificate__Password`.

No credential belongs in `appsettings.json` — that file is in the repository. It ships with an
empty `Users` array on purpose: **configuration arrays are merged per index, not replaced**. An
account defined there would survive every later source, unless that source happened to overwrite
the very same index. The accounts are therefore defined once, in user secrets or in the
production file, starting at index 0:

```
dotnet user-secrets set "MqttBrokerSettings:Users:0:UserName" "core-service"
dotnet user-secrets set "MqttBrokerSettings:Users:0:Password" "..."
```

**Put every name clients actually dial into `HostNames`**, including IP addresses. Full
certificate validation reads the subject alternative names; a name that is not in there fails
even though the certificate is otherwise perfectly valid. Clients that cannot validate through a
certificate authority pin the fingerprint instead — the broker logs it on every start.

## Deploying a new build

```powershell
dotnet publish -c Release -o C:\Temp\MqttBrokerPublish
.\deploy\deploy.ps1 -Source 'C:\Temp\MqttBrokerPublish' -Target 'C:\Services\MqttBroker'
```

The service is stopped for the copy — its own DLLs are locked while it runs — so clients
reconnect. That is a few seconds, and it now only happens when the *broker* is deployed.

## Service account

Start with `LocalSystem` and change the account only once the broker is running. The certificate
is loaded with `X509KeyStorageFlags.UserKeySet`, which needs a user key container; a virtual
account (`NT SERVICE\MqttBroker`) has no loaded profile and the load can fail there. Changing the
account first turns a configuration problem into a certificate problem and costs an evening.

## Watchdog

The library logs a broker that fails to start and carries on — sensible for an application that
has other work, wrong for this service, which has none. A process that is up without a listening
broker looks healthy to monitoring while no client can connect.

`BrokerWatchdog` therefore checks the listener table for the configured ports and stops the
service when nobody is listening, leaving the restart to the Windows recovery policy. It reads the
listener table rather than opening a connection, so it does not fill the broker's log with its own
aborted handshakes.

Defaults: first check after 45 seconds (the library retries binding ten times, three seconds
apart), then every 60 seconds, stopping after two consecutive misses. `WatchdogSettings:Enabled`
turns it off while debugging.
