using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace MqttServices.Core.Broker;

/// <summary>
/// Supplies the TLS certificate the broker presents: loaded from disk when a path is configured,
/// generated otherwise.
/// </summary>
public static class BrokerCertificateProvider
{
    /// <summary>TLS server authentication.</summary>
    private const string ServerAuthenticationOid = "1.3.6.1.5.5.7.3.1";

    /// <summary>
    /// Returns the certificate to use, creating and storing one if a path is configured but no
    /// file exists there yet, and replacing a stored one that has expired.
    /// </summary>
    public static X509Certificate2 GetOrCreate(BrokerCertificateSettings settings, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var path = settings.Path?.Trim() ?? "";
        if (path.Length == 0)
        {
            var ephemeral = Create(settings);
            logger?.LogWarning(
                "The broker certificate is generated anew on every start, so its fingerprint " +
                "changes with every restart and pinned clients have to be reconfigured. Set " +
                "MqttBrokerSettings:Certificate:Path to keep it stable.");
            return ephemeral;
        }

        if (File.Exists(path))
        {
            var stored = Load(path, settings.Password, logger);
            if (stored is not null)
            {
                if (stored.NotAfter > DateTime.Now)
                {
                    logger?.LogInformation(
                        "Loaded the broker certificate from {Path}. Valid until {NotAfter:u}.",
                        path,
                        stored.NotAfter);
                    return stored;
                }

                // Keeping it would mean every client refuses the broker; replacing it changes the
                // fingerprint, which is why a generated certificate is given a long validity.
                logger?.LogWarning(
                    "The broker certificate in {Path} expired on {NotAfter:u} and is being replaced. " +
                    "Its fingerprint changes, so pinned clients have to be reconfigured.",
                    path,
                    stored.NotAfter);
                stored.Dispose();
            }
        }

        var certificate = Create(settings);
        Store(certificate, path, settings.Password, logger);
        return certificate;
    }

    private static X509Certificate2? Load(string path, string? password, ILogger? logger)
    {
        try
        {
            return X509CertificateLoader.LoadPkcs12FromFile(
                path,
                string.IsNullOrEmpty(password) ? null : password,
                X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable);
        }
        catch (CryptographicException ex)
        {
            // A wrong password and a damaged file look the same from here. Either way, silently
            // generating a replacement would overwrite a certificate the operator may still need,
            // so refuse instead of guessing.
            throw new InvalidOperationException(
                $"The broker certificate in '{path}' could not be read. If the file is intact, " +
                "check MqttBrokerSettings:Certificate:Password. Remove or rename the file to have " +
                "a new certificate generated.",
                ex);
        }
        catch (IOException ex)
        {
            logger?.LogError(ex, "The broker certificate in {Path} could not be read.", path);
            throw;
        }
    }

    private static void Store(X509Certificate2 certificate, string path, string? password, ILogger? logger)
    {
        var directory = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var pfx = string.IsNullOrEmpty(password)
            ? certificate.Export(X509ContentType.Pfx)
            : certificate.Export(X509ContentType.Pfx, password);

        File.WriteAllBytes(path, pfx);
        RestrictToOwner(path, logger);

        logger?.LogInformation(
            "Generated a broker certificate and stored it in {Path}. Its fingerprint stays stable " +
            "across restarts from now on.",
            path);

        if (string.IsNullOrEmpty(password))
        {
            logger?.LogWarning(
                "The broker certificate in {Path} holds an unprotected private key. Set " +
                "MqttBrokerSettings:Certificate:Password, or make sure the file system keeps the " +
                "file away from other users.",
                path);
        }
    }

    /// <summary>
    /// The file holds a private key, so take away group and world access where the platform has
    /// the concept. On Windows the ACL of the containing directory applies instead.
    /// </summary>
    private static void RestrictToOwner(string path, ILogger? logger)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Could not restrict the permissions of {Path} to its owner.", path);
        }
    }

    private static X509Certificate2 Create(BrokerCertificateSettings settings)
    {
        var subjectName = string.IsNullOrWhiteSpace(settings.SubjectName) ? "localhost" : settings.SubjectName.Trim();

        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddIpAddress(IPAddress.Loopback);
        sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
        sanBuilder.AddDnsName("localhost");

        // The subject common name alone is not enough: validation reads the subject alternative
        // names, so whatever clients dial has to appear in there.
        foreach (var name in settings.HostNames.Concat([subjectName]))
        {
            AddSubjectAlternativeName(sanBuilder, name);
        }

        using var rsa = RSA.Create(2048);
        var certRequest = new CertificateRequest(
            $"CN={subjectName}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        certRequest.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature,
                false));

        certRequest.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(new OidCollection { new(ServerAuthenticationOid) }, false));

        certRequest.CertificateExtensions.Add(sanBuilder.Build());

        var validityDays = settings.ValidityDays > 0 ? settings.ValidityDays : 3650;
        using var created = certRequest.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(validityDays));

        // Round-tripping through PFX is what attaches the private key in a form the TLS stack can
        // use on every platform.
        return X509CertificateLoader.LoadPkcs12(
            created.Export(X509ContentType.Pfx),
            (string?)null,
            X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable);
    }

    private static void AddSubjectAlternativeName(SubjectAlternativeNameBuilder sanBuilder, string? name)
    {
        var value = name?.Trim() ?? "";
        if (value.Length == 0 || value == "localhost")
        {
            return;
        }

        if (IPAddress.TryParse(value, out var address))
        {
            sanBuilder.AddIpAddress(address);
        }
        else
        {
            sanBuilder.AddDnsName(value);
        }
    }
}
