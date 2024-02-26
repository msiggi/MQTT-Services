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

    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var payloadPerson = new Person
        {
            Name = "Test from Agent1 with Request-Payload"
        };
        var payloadPerson2 = new Person
        {
            Name = "Second Test"
        };

        Thread.Sleep(2000);

        await messagingManager.SendMessageRequest<Person>(payloadPerson, "extest");
        //await messagingManager.SendMessageRequest<Person>(payloadPerson2, "extest");

        // or without payload, just to trigger the answer:
        //await messagingManager.SendMessageRequest(payload, "test");
        //Thread.Sleep(1000);
        //await messagingManager.SendMessageRequest(payload2, "test");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

