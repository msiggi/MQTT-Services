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
       // services.AddSingleton<IHostedService, MqttBrokerService>();
        services.AddSingleton<MqttBrokerService>();
        //services.AddSingleton<IHostedService>(s => s.GetService<MqttBrokerService>());


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
}
