
using MqttServices.Core.Services;

namespace SampleBlazorAppWithMqttClient.Services;

public class TestBackgroundService : IHostedService, IDisposable
{
    private readonly IMessagingManager messagingManager;

    public TestBackgroundService(IMessagingManager messagingManager)
    {
        this.messagingManager = messagingManager;
        this.messagingManager.RequestReceived += MessagingManager_RequestReceived;
    }

    private void MessagingManager_RequestReceived(object? sender, MqttServices.Core.Common.Payload e)
    {
        throw new NotImplementedException();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    public void Dispose()
    {
        this.messagingManager.RequestReceived -= MessagingManager_RequestReceived;
    }

}
