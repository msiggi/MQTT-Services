using SampleCommon;

namespace SampleClientMessaging1;

public class Worker : IHostedService
{
    private readonly ILogger<Worker> logger;
    private readonly IMessagingManager messagingManager;

    public Worker(ILogger<Worker> logger, IMessagingManager messagingManager)
    {
        this.logger = logger;
        this.messagingManager = messagingManager;
        this.messagingManager.ResponseReceived += MessagingManager_ResponseReceived;
    }

    private void MessagingManager_ResponseReceived(object? sender, Payload e)
    {
        logger.LogInformation($"ResponseReceived with Name {e.Name} received!");
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await messagingManager.SendMessageRequest(new Payload
        {
            Name = "Test from Agent1"
        });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
