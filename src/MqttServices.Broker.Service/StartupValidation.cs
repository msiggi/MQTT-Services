using MqttServices.Core.Broker;

namespace MqttServices.Broker.Service;

/// <summary>
/// Refuses a configuration that would start a broker nobody can use.
/// </summary>
public static class StartupValidation
{
    public static void Validate(MqttBrokerSettings settings)
    {
        if (!settings.EnableBroker)
        {
            throw new InvalidOperationException(
                "MqttBrokerSettings:EnableBroker is false. This service does nothing else, so " +
                "running it with the broker switched off is a configuration mistake rather than " +
                "a state worth keeping alive.");
        }

        if (settings.Users.Count == 0)
        {
            throw new InvalidOperationException(
                "MqttBrokerSettings:Users is empty. The shipped appsettings.json holds " +
                "placeholders only; put the real accounts in appsettings.Production.json in the " +
                "data directory.");
        }

        var placeholders = settings.Users
            .Where(user => string.IsNullOrWhiteSpace(user.Password) || user.Password == "changeme")
            .Select(user => user.UserName)
            .ToList();

        if (placeholders.Count > 0)
        {
            throw new InvalidOperationException(
                $"These broker accounts still carry a placeholder password: {string.Join(", ", placeholders)}. " +
                "Set real passwords in appsettings.Production.json in the data directory.");
        }
    }
}
