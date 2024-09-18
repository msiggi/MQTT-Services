
using MqttServices.Core.Services;
using Optilog.Common.Services.Configurations;
using SampleCommon;

namespace SampleRemoteResponder;

public class Worker : IHostedService
{
    private readonly ILogger<Worker> _logger;
    private readonly IRemoteCallResponder<PersonData> remoteCallResponder;

    public Worker(ILogger<Worker> logger, IRemoteCallResponder<PersonData> remoteCallResponder)
    {
        _logger = logger;
        this.remoteCallResponder = remoteCallResponder;
        this.remoteCallResponder.GetAllItemsRequestReceived += RemoteCallResponder_GetAllItemsRequestReceived;
    }

    private async void RemoteCallResponder_GetAllItemsRequestReceived(object? sender, RequestFilter e)
    {
        await remoteCallResponder.SendAllItems(new List<PersonData> 
        {
            new PersonData { Name = "Jimi Hendrix" },
            new PersonData { Name = "Janis Joplin" },
            new PersonData { Name = "Jim Morrison" },
            new PersonData { Name = "Kurt Cobain" }
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
