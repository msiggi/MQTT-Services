namespace MqttServices.Core.Broker
{
    public interface IMqttBrokerService
    {
        Task StartBroker();

        /// <summary>
        /// SHA-256 fingerprint of the certificate this broker presents, or <c>null</c> before the
        /// broker has started. Clients that pin this broker need the value for
        /// <c>MqttClientSettings.TrustedCertificateThumbprint</c>; exposing it here saves reading
        /// it out of the log, so a host application can show it in its own UI or API.
        /// </summary>
        string? CertificateThumbprint => null;
    }
}