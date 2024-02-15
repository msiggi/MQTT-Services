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
        var payload = new Payload
        {
            Name = "Test from Agent1 with Request-Payload"
        };
        var payload2 = new Payload
        {
            Name = "Second Test"
        };
        await messagingManager.SendMessageRequest(payload);

        // or without payload, just to trigger the answer:
        await messagingManager.SendMessageRequest(payload, "test");
        Thread.Sleep(1000);
        await messagingManager.SendMessageRequest(payload2, "test");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
