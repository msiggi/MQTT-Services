using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MqttServices.Core.Broker;
using MqttServices.Core.Client;

//namespace MqttServices.Core.Common;

public static class ServiceCollectionsExtensions
{
    public static IServiceCollection AddMqttBrokerService(this IServiceCollection services, Action<MqttBrokerSettings> setupAction)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (setupAction == null) throw new ArgumentNullException(nameof(setupAction));

        services.Configure(setupAction);
        services.AddSingleton<MqttBrokerService>();

        return services;
    }

    public static IServiceCollection AddMqttBrokerService(this IServiceCollection services, MqttBrokerSettings mqttBrokerSettings)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        services.AddOptions<MqttBrokerSettings>().Configure(options =>
        {
            options.EnableBroker = mqttBrokerSettings.EnableBroker;
            options.Users = mqttBrokerSettings.Users;
            options.DelayInMilliSeconds = mqttBrokerSettings.DelayInMilliSeconds;
            options.Port = mqttBrokerSettings.Port;
            options.TlsPort = mqttBrokerSettings.TlsPort;
        });
        services.AddSingleton<MqttBrokerService>();

        return services;
    }

    public static IServiceCollection AddMqttClientService(this IServiceCollection services, Action<MqttClientSettings> setupAction)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (setupAction == null) throw new ArgumentNullException(nameof(setupAction));

        services.Configure(setupAction);
        services.AddSingleton<MqttClientService>();

        return services;
    }

    public static IServiceCollection AddMqttClientService(this IServiceCollection services, MqttClientSettings mqttClientSettings)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        services.AddOptions<MqttClientSettings>().Configure(options =>
        {
            options.BrokerPort = mqttClientSettings.BrokerPort;
            options.BrokerHost = mqttClientSettings.BrokerHost;
            options.UserName = mqttClientSettings.UserName;
            options.Password = mqttClientSettings.Password;
            options.ServiceName = mqttClientSettings.ServiceName;
        });
        services.AddSingleton<MqttClientService>();

        return services;
    }
}
