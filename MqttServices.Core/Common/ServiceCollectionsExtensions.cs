using Microsoft.Extensions.DependencyInjection;
using MqttServices.Core.Broker;
using MqttServices.Core.Client;
using MqttServices.Core.Services;

//namespace MqttServices.Core.Common;

public static class ServiceCollectionsExtensions
{
    public static IServiceCollection AddMqttBrokerService(this IServiceCollection services, Action<MqttBrokerSettings> setupAction)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (setupAction == null) throw new ArgumentNullException(nameof(setupAction));

        services.Configure(setupAction);
        services.AddSingleton<IMqttBrokerService, MqttBrokerService>();

        // Resolve the service to create an instance
        var serviceProvider = services.BuildServiceProvider();
        var brokerService = serviceProvider.GetRequiredService<IMqttBrokerService>();

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
        services.AddSingleton<IMqttBrokerService, MqttBrokerService>();

        return services;
    }
    /// <summary>
    /// Adds the MqttClientService to the service collection.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="setupAction"></param>
    /// <param name="exchangeTopicPrefix">An unique string to distinguish the remote calls to other applications. It must be euqal for all applications which
    /// must call to each other</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IServiceCollection AddMqttClientService(this IServiceCollection services, Action<MqttClientSettings> setupAction, string exchangeTopicPrefix = "mqttservices")
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (setupAction == null) throw new ArgumentNullException(nameof(setupAction));

        services.Configure(setupAction);
        services.AddSingleton<IMqttClientService, MqttClientService>();
        return services;
    }

    public static IServiceCollection AddMqttMessagingService(this IServiceCollection services, Action<MqttClientSettings> setupAction)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (setupAction == null) throw new ArgumentNullException(nameof(setupAction));

        services.Configure(setupAction);
        services.AddSingleton<IMqttClientService>(provider =>
            (IMqttClientService)ActivatorUtilities.CreateInstance(provider, typeof(MqttClientService)));
        services.AddSingleton<IMessagingManager>(provider =>
            (IMessagingManager)ActivatorUtilities.CreateInstance(provider, typeof(MessagingManager)));

        services.AddHostedService<MqttClientReconnectService>();
        return services;
    }

    public static IServiceCollection AddMqttClientService(this IServiceCollection services, MqttClientSettings mqttClientSettings)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        services.AddOptions<MqttClientSettings>().Configure(options =>
        {
            options.Enabled = mqttClientSettings.Enabled;
            options.BrokerPort = mqttClientSettings.BrokerPort;
            options.BrokerHost = mqttClientSettings.BrokerHost;
            options.UserName = mqttClientSettings.UserName;
            options.Password = mqttClientSettings.Password;
            options.ServiceName = mqttClientSettings.ServiceName;
        });
        services.AddSingleton<IMqttClientService, MqttClientService>();

        return services;
    }
}
