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
        Thread.Sleep(2000);

        var payloadPerson = new PersonDataRequest
        {
            PersonId = 4711
        };
        await messagingManager.SendMessageRequest<PersonDataRequest>(payloadPerson, "persontestrequest");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

