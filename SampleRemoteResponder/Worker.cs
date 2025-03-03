
using MqttServices.Core.Services;
using SampleCommon;

namespace SampleRemoteResponder;

public class Worker : IHostedService
{
    private readonly ILogger<Worker> _logger;
    private readonly IRemoteCallResponder<GuitarPlayer> remoteCallResponder;

    public Worker(ILogger<Worker> logger, IRemoteCallResponder<GuitarPlayer> remoteCallResponder)
    {
        _logger = logger;
        this.remoteCallResponder = remoteCallResponder;
        this.remoteCallResponder.GetAllItemsRequestReceived += RemoteCallResponder_GetAllItemsRequestReceived;
    }

    private async void RemoteCallResponder_GetAllItemsRequestReceived(object? sender, RequestFilter e)
    {
        await remoteCallResponder.SendAllItems(new List<GuitarPlayer> 
        {
            new GuitarPlayer { Name = "Jimi Hendrix" },
            new GuitarPlayer { Name = "Janis Joplin" },
            new GuitarPlayer { Name = "Jim Morrison" },
            new GuitarPlayer { Name = "Kurt Cobain" }
        });
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
