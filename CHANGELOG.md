# Changelog

All notable changes to this project are documented here. Versions follow
[Semantic Versioning](https://semver.org/).

## [4.0.0-beta.1] - 2026-09-15

The MQTT client validates the broker's certificate from this version on. Up to 3.x it accepted any
certificate the broker presented, which protects against passive eavesdropping only: a
man-in-the-middle presents their own certificate, the client takes it, and the broker credentials
and all payloads are readable in clear text.

### Breaking

- **The client validates the broker certificate.** Three tiers, in this order of precedence:
  - `AllowUntrustedCertificates: true` accepts anything — the behaviour of 3.x, kept as a
    deliberate escape hatch.
  - `TrustedCertificateThumbprint` accepts exactly the certificate with that SHA-256 fingerprint
    ("pinning"). This needs no certificate authority and is still safe against a
    man-in-the-middle. It is the tier for a self-signed broker on a LAN.
  - Neither set: full validation of chain, host name and validity period.

  **What to do when upgrading:** a broker with a certificate from a certificate authority, reached
  by the host name it was issued for, keeps working unchanged. A self-signed broker, or one reached
  by IP address, needs its fingerprint in `TrustedCertificateThumbprint`; the broker logs the
  fingerprint on startup.

- **`TlsVersion` defaults to the operating system's choice** instead of a fixed `"1.2"`, on both the
  client and the broker. It negotiates the highest version both sides support and applies the
  machine's own policy. An explicit `"1.0"`–`"1.3"` is still honoured; an unknown value is now
  logged and falls back to the system default instead of silently becoming `"1.2"`.

- **`EncryptWithTls` is applied.** It existed but was never read, so TLS could not be switched off
  at all. Setting it to `false` now really does connect without TLS.

- **`IMqttBrokerService` gained `CertificateThumbprint`.** It has a default implementation, so
  existing implementers keep compiling.

### Added

- `MqttClientSettings.TrustedCertificateThumbprint` and `MqttClientSettings.AllowUntrustedCertificates`.
- `MqttBrokerSettings.Certificate` with `Path`, `Password`, `SubjectName`, `HostNames` and
  `ValidityDays`. With a `Path` set, the broker stores its self-signed certificate as a PFX file and
  loads it again on the next start, so the fingerprint survives restarts and clients can pin it.
  Without a path the certificate is generated in memory on every start, as before.
- `IMqttBrokerService.CertificateThumbprint`, so a host application can show the fingerprint instead
  of having operators read it out of the log.
- The broker logs the subject and SHA-256 fingerprint of its certificate on startup.

### Changed

- A rejected certificate is logged with the reason. The host name mismatch names the way out, since
  reaching a broker by IP or by an mDNS name such as `raspberrypi.local` fails even with a good
  certificate and is the most common stumbling block. A failed TLS handshake is reported as such; a
  bare `Result-Code` was never enough to diagnose one.
- Configured fingerprints are read in whatever shape they were copied from: with colons (OpenSSL),
  with spaces (the Windows certificate dialog, which also prefixes an invisible left-to-right mark)
  or as plain hex. A value that is not 64 hex digits is rejected outright rather than silently never
  matching.
- The repository is laid out as `src/`, `tests/` and `samples/`; the solution moved to the SLNX
  format. No effect on the package.

### Fixed

- The README documented `TlsVersion` values (`Tls12` and the like) that the code never accepted —
  it maps `"1.0"`–`"1.3"` — and did not list the client's TLS settings at all.

### Notes for this beta

Verified end to end against the integrated broker and against a public broker with a certificate
from a real certificate authority. Please report anything that does not connect after following the
upgrade notes above.

## [3.0.1]

- MQTTnet diagnostics routed through `ILogger` instead of the console.

## [3.0.0]

- .NET 10, MQTTnet 5.

Earlier versions are not documented here; see the git history.
