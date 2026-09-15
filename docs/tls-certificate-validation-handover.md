# Übergabe: Zertifikatsprüfung im MQTT-Client

**Stand:** 2026-09-14 · **Betrifft:** `src/MqttServices.Core/Client/` · **Gefunden bei:** Argus-Hub-Deployment auf argus.siegelnet.de

> **Status: umgesetzt am 2026-09-15.** Die drei Stufen stehen in
> `MqttCertificateValidator.cs`, die Benutzerdokumentation im README unter „TLS and certificate
> validation". Dieses Dokument bleibt als Analyse stehen. Zwei Punkte daraus sind erledigt, zwei
> offen:
>
> - **Erledigt:** `WithTlsOptions(...)` setzt `UseTls` in MQTTnet 5.2.0.1603 tatsächlich implizit
>   auf `true` — nachgemessen, nicht geraten. `EncryptWithTls` wird jetzt über `o.UseTls(...)`
>   ausgewertet und ist damit keine tote Property mehr.
> - **Erledigt:** Der Handler bleibt bestehen statt wegzufallen (siehe „Umsetzungshinweise"). Nur
>   so lassen sich die Fehlschläge mit einer brauchbaren Meldung loggen; das Standardverhalten von
>   MQTTnet meldet lediglich, dass der Handshake scheiterte.
> - **Offen:** Der mitgelieferte Broker erzeugt bei **jedem Start** ein neues Zertifikat. Der
>   Fingerabdruck ändert sich damit bei jedem Neustart, und Stufe 2 ist für ihn nur innerhalb
>   einer Broker-Laufzeit brauchbar. Broker-seitige Persistenz fehlt.
> - **Erledigt, aber anders als vorgeschlagen:** Der Default von `TlsVersion` ist nicht `"1.3"`,
>   sondern `""` bzw. `"auto"` — die Aushandlung übernimmt das Betriebssystem. Eine fest
>   verdrahtete Version altert in beide Richtungen schlecht: sie sperrt ein neueres Protokoll aus,
>   das die Maschine schon kann, und hält ein überholtes am Leben, nachdem das System es
>   ausgemustert hat. Die Mapping-Logik lag doppelt in Client und Broker und steht jetzt in
>   `Common/TlsVersions.cs`; ein unbekannter Wert wird geloggt statt still zu `"1.2"` zu werden.

## Worum es geht

Der Client verschlüsselt jede Verbindung mit TLS, prüft das Zertifikat der Gegenstelle aber
nicht. Verschlüsselung ohne Prüfung schützt gegen passives Mitlesen, nicht gegen einen
Man-in-the-Middle: wer sich dazwischenschiebt, legt ein eigenes Zertifikat vor, der Client
akzeptiert es, und der Angreifer sieht Broker-Zugangsdaten und alle Nutzdaten im Klartext.

Im LAN mit selbstsigniertem Broker ist das nachvollziehbar — vermutlich ist der Handler genau
dafür entstanden. Für Clients, die sich über das Internet verbinden, ist es die falsche
Voreinstellung.

## Die Fundstellen

**`src/MqttServices.Core/Client/MqttClientService.cs`, ca. Zeile 88–97** — der Verbindungsaufbau:

```csharp
var mqttClientOptions = new MqttClientOptionsBuilder()
    .WithTcpServer(mqttClientSettings.BrokerHost, mqttClientSettings.BrokerPort)
    .WithTlsOptions(o =>
    {
        o.WithCertificateValidationHandler(_ => true);   // <- akzeptiert jedes Zertifikat
        o.WithSslProtocols(sslVersion);
    })
    .WithCredentials(mqttClientSettings.UserName, mqttClientSettings.Password)
    .WithClientId(mqttClientSettings.ServiceName + Guid.NewGuid().ToString())
    .Build();
```

**`src/MqttServices.Core/Client/MqttClientSettings.cs`** — hier kommen die neuen Optionen hin.
Zwei vorhandene Properties als Kontext:

- `TlsVersion` (Default `"1.2"`) wird über die Hilfs-Property `TlsVersion` in
  `MqttClientService.cs` Zeile 24–35 auf `SslProtocols` gemappt. `"1.3"` wäre ein guter neuer
  Default, sobald die Prüfung steht.
- `EncryptWithTls` (Default `true`) ist **tot**: gesetzt in
  `Common/ServiceCollectionsExtensions.cs:97`, im Verbindungsaufbau aber nie gelesen. TLS lässt
  sich also gar nicht abschalten. Entweder auswerten oder entfernen — so ist es irreführend.

**`src/MqttServices.Core/Broker/MqttBrokerService.cs`, ca. Zeile 94** — der mitgelieferte Broker
erzeugt ein **selbstsigniertes** Zertifikat auf `CN=localhost`. Das ist der Grund, warum eine
harte Umstellung auf volle Prüfung jede LAN-Installation brechen würde: `localhost` passt
nicht zum Hostnamen, mit dem ein Client von einer anderen Maschine aus verbindet.

## Geplantes Zielbild: drei Stufen

| Konfiguration | Verhalten | Wofür |
|---|---|---|
| nichts gesetzt | volle Prüfung (Kette, Hostname, Gültigkeit) | öffentlicher Broker mit CA-Zertifikat |
| `TrustedCertificateThumbprint` gesetzt | genau dieses eine Zertifikat, sonst nichts | selbstsignierter Broker im LAN |
| `AllowUntrustedCertificates: true` | heutiges Verhalten | Notausgang, bewusst gewählt |

Der mittlere Weg ("Pinning") ist der interessante: er braucht **keine** Zertifizierungsstelle
und ist trotzdem MITM-sicher, weil ein untergeschobenes Zertifikat einen anderen Fingerabdruck
hat. Für den mitgelieferten Broker mit `CN=localhost` ist er der passende Weg.

## Umsetzungshinweise

Die Signatur ist `WithCertificateValidationHandler(Func<MqttClientCertificateValidationEventArgs, bool>)`.
Im Argument stecken `Certificate`, `Chain` und `SslPolicyErrors` — genug für alle drei Stufen:

- **Volle Prüfung** heißt schlicht: `args.SslPolicyErrors == SslPolicyErrors.None`.
  Alternativ den Handler weglassen, dann prüft MQTTnet selbst mit dem Standardverhalten. Das
  ist der sauberere Weg, macht die drei Stufen aber unsymmetrisch — beides mal ausprobieren.
- **Pinning**: `args.Certificate.GetCertHashString(HashAlgorithmName.SHA256)` gegen den
  konfigurierten Wert vergleichen, case-insensitiv und ohne Leerzeichen/Doppelpunkte
  (Windows zeigt Fingerabdrücke mit Trennzeichen an — das Normalisieren nicht vergessen,
  sonst schlägt der Vergleich aus einem sehr dummen Grund fehl).
- Der Vergleich sollte zeitkonstant sein? **Nein** — ein Fingerabdruck ist öffentlich, kein
  Geheimnis. Hier ist ein normaler Vergleich richtig.

**Achtung beim Hostnamen:** die volle Prüfung schlägt fehl, wenn `BrokerHost` nicht im
Zertifikat steht. Eine IP-Adresse oder ein mDNS-Name wie `raspberrypi.local` scheitert auch bei
einem gültigen Zertifikat. Das ist korrektes Verhalten, aber der häufigste Stolperstein — eine
klare Log-Meldung an dieser Stelle spart später viel Sucherei. Heute loggt der Client bei
Fehlschlag nur `Result-Code`, was für ein TLS-Problem zu wenig ist.

## Verifikation

1. **Pinning:** mitgelieferten Broker starten, Fingerabdruck des erzeugten Zertifikats
   auslesen, in die Client-Settings eintragen → Verbindung klappt. Eine Ziffer im Fingerabdruck
   ändern → Verbindung wird abgelehnt.
2. **Volle Prüfung:** gegen einen Broker mit CA-Zertifikat verbinden (z. B. der Broker hinter
   `api.thingsbridge.net`, Let's-Encrypt-Zertifikat via win-acme) → klappt. Denselben Broker
   über seine IP-Adresse statt über den Hostnamen ansprechen → muss fehlschlagen.
3. **Notausgang:** `AllowUntrustedCertificates: true` → beide obigen Fälle verbinden sich.

## Offener Punkt aus der Analyse

Ob `WithTlsOptions(...)` in MQTTnet 5.2.0.1603 `UseTls` implizit auf `true` setzt, wurde
**nicht** verifiziert (Testprojekt kam nicht zustande). Indirekter Beleg: ein Argus-Node
verbindet sich erfolgreich auf `api.thingsbridge.net:18883`. Falls dieser Port TLS-only ist,
ist die Frage beantwortet. Andernfalls beim Umbau kurz explizit prüfen — und `UseTls` im
Zweifel ausdrücklich setzen, statt sich auf einen Default zu verlassen.

## Breaking Change

Bestehende Installationen müssen nach dem Umbau einmalig eine der drei Stufen wählen. Das ist
der Preis dafür, dass der heutige Zustand einer ist, bei dem niemand merkt, dass nicht geprüft
wird. Für das README lohnt ein kurzer Migrationsabschnitt: "wer bisher gegen einen
selbstsignierten Broker verbunden hat, trägt jetzt den Fingerabdruck ein — so liest man ihn
aus: …".
