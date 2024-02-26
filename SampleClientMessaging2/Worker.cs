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
        if (e.ExchangeName == "persontestrequest")
        {
            var person = (PersonDataRequest)e.Value;
            var personId = person.PersonId;
            // get Person per Id
            // .................

            PersonDataResponse personResponse = new PersonDataResponse
            {
                PersonId = personId,
                Name = "Max Mustermann",
                Birthday = new DateTime(1999, 4, 1)
            };

            messagingManager.SendMessageResponse<PersonDataResponse>(personResponse, e.ExchangeName);
        }

        logger.LogInformation($"RequestReceived with ExchangeName {e.ExchangeName} received, sending Answer...!");

        messagingManager.SendMessageResponse(
            new Payload
            {
                ExchangeName = e.ExchangeName

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
