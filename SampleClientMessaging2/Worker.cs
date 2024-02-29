using MqttServices.Core.Common;
using MqttServices.Core.Services;
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
        if (e.ExchangeName == Configs.personExchangeName)
        {
            var person = (PersonDataRequest)e.Value;
            logger.LogInformation($"Request Received, Person {person.PersonId} requested, sending Answer...!");

            var personId = person.PersonId;

            // *****************************************
            // get complete Person per Id from elsewhere
            // .................
            // *****************************************

            PersonDataResponse personResponse = new PersonDataResponse
            {
                PersonId = personId,
                Name = "Max Mustermann",
                Birthday = new DateTime(1999, 4, 1)
            };

            messagingManager.SendMessageResponse<PersonDataResponse>(personResponse, e.ExchangeName);
        }

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
