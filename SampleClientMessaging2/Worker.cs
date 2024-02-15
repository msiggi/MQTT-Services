using SampleCommon;

namespace SampleClientMessaging2;

public class Worker : IHostedService
{
    private readonly ILogger<Worker> logger;
    private readonly IMessagingManager messagingManager;

    public Worker(ILogger<Worker> logger, IMessagingManager messagingManager)
    {
        this.logger = logger;
        this.messagingManager = messagingManager;
        this.messagingManager.RequestReceived += MessagingManager_RequestReceived;
    }

    private void MessagingManager_RequestReceived(object? sender, Payload e)
    {
        logger.LogInformation($"RequestReceived with Name {e.Name} received, sending Answer...!");
        messagingManager.SendMessageResponse(new Payload { Name = "Resonse!" });
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
