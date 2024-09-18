using MqttServices.Core.Services;
using SampleCommon;

namespace SampleRemoteRequester;

public class Worker : IHostedService
{
    private readonly ILogger<Worker> logger;
    private readonly IRemoteCallRequester<PersonData> remoteServiceRequester;

    public Worker(ILogger<Worker> logger, IRemoteCallRequester<PersonData> remoteServiceRequester)
    {
        this.logger = logger;
        this.remoteServiceRequester = remoteServiceRequester;
        this.remoteServiceRequester.AllItemsResponseReceived += RemoteServiceRequester_AllItemsResponseReceived;
    }

    private void RemoteServiceRequester_AllItemsResponseReceived(object? sender, List<PersonData> e)
    {
        foreach (var person in e)
        {
            logger.LogInformation($"{person.Name} received!");
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Worker running at: {time}", DateTime.Now);

        Thread.Sleep(2000); // lets start the responder first

        await remoteServiceRequester.RequestAllItems();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
