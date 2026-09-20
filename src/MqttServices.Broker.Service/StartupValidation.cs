using Microsoft.Extensions.Hosting;
using MqttServices.Core.Broker;

namespace MqttServices.Broker.Service;

/// <summary>
/// Refuses a configuration that would start a broker nobody can use.
/// </summary>
public static class StartupValidation
{
    public static void Validate(MqttBrokerSettings settings, IHostEnvironment environment)
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
                "MqttBrokerSettings:Users is empty. Configure the accounts in user secrets " +
                "during development, or in appsettings.Production.json in the data directory on " +
                "a server. The shipped appsettings.json deliberately carries no account: " +
                "configuration arrays are merged per index rather than replaced, so an entry " +
                "there would survive every later source that does not overwrite that exact " +
                "index." + UserSecretsHint(environment));
        }

        var placeholders = settings.Users
            .Where(user => string.IsNullOrWhiteSpace(user.Password) || user.Password == "changeme")
            .Select(user => user.UserName)
            .ToList();

        if (placeholders.Count > 0)
        {
            throw new InvalidOperationException(
                $"These broker accounts have no usable password: {string.Join(", ", placeholders)}. " +
                "Configuration arrays are merged per index rather than replaced, so an account " +
                "defined further up -- in appsettings.json, say -- stays in the list unless a " +
                "later source overwrites that same index. Check which index each account sits " +
                "on: MqttBrokerSettings:Users:0:UserName, MqttBrokerSettings:Users:1:UserName " +
                "and so on." + UserSecretsHint(environment));
        }
    }

    /// <summary>
    /// User secrets are read in the Development environment and nowhere else, which is easy to
    /// trip over when the accounts are in there and the environment is not set.
    /// </summary>
    private static string UserSecretsHint(IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            return "";
        }

        return $" Note that the environment is '{environment.EnvironmentName}': user secrets are " +
               "read in Development only, so accounts configured there are not being loaded. Set " +
               "DOTNET_ENVIRONMENT=Development to use them.";
    }
}
