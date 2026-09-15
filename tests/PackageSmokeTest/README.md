# Package smoke test

Checks a **published** `MQTT-Services` package through its public API, the way a consumer
would. Run it after a release, before telling anyone the version works.

```powershell
./run.ps1                        # the version pinned in smoketest.cs
./run.ps1 -Version 4.0.0-beta.2  # any other published version
```

Or directly, without the wrapper:

```bash
dotnet run smoketest.cs
```

## What it covers

The unit tests in `MqttServices.CoreTests` compile against the source through a
`ProjectReference`. They cannot see anything that goes wrong between the source and the
package a consumer installs — a missing dependency in the nuspec, a type that ended up
`internal`, a wrong target framework, a broken pack step. This runs against the real
artifact, downloaded from nuget.org.

It hosts the broker and the client in one process, both from the package, so it needs no
infrastructure. That also makes it possible to check the case that matters most: a
certificate that is perfectly valid but not the expected one, which no external broker
would hand out on request.

| Check | Why |
|---|---|
| the fingerprint is exposed on `IMqttBrokerService` | the API a host application uses |
| the certificate was written to disk | persistence is configured correctly |
| pinned, correct fingerprint connects | the ordinary case |
| pinned, one digit changed is rejected | proves validation actually happens |
| full validation rejects a self-signed broker | the default tier works |
| `AllowUntrustedCertificates` connects | the escape hatch works |
| the OpenSSL colon format is accepted | fingerprints get copied in many shapes |
| a message survives the round trip | the library works, not only the TLS layer |
| the fingerprint is unchanged after a restart | pinned clients survive a broker restart |
| the unchanged pinned client reconnects | the point of the whole exercise |

## Why there is no .csproj

A file-based app keeps this out of `**/*.csproj`, which the release pipeline restores and
builds. A project here would reference a package version that does not exist yet at the
moment that version is being built, and would break the release.

The version therefore lives in the `#:package` directive at the top of `smoketest.cs`.
Bump it together with `<Version>` in `MqttServices.Core.csproj`, or pass `-Version` to
`run.ps1` to try one without editing anything.
