using MqttServices.Core.Common;
using MqttServices.Core.Services;
using SampleCommon;

namespace SampleClientMessaging1;

public class RequestWorker : IHostedService
{
    private readonly ILogger<RequestWorker> logger;
    private readonly IMessagingManager messagingManager;

    public RequestWorker(ILogger<RequestWorker> logger, IMessagingManager messagingManager)
    {
        this.logger = logger;
        this.messagingManager = messagingManager;
        this.messagingManager.ResponseReceived += MessagingManager_ResponseReceived;
    }

    private void MessagingManager_ResponseReceived(object? sender, Payload e)
    {
        if (e.ExchangeName == Configs.personExchangeName)
        {
            PersonDataResponse personDataResponse = (PersonDataResponse)e.Value;
            logger.LogInformation($"**** Response Received with Person {personDataResponse.PersonData.Name}!");
        }
        if (e.ExchangeName == Configs.cityExchangeName)
        {
            logger.LogInformation($"**** Response Received with City {((AddressData)e.Value).CityName}!");
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Thread.Sleep(2000);

        // Test Simple Messaging
        var payloadPerson = new PersonData
        {
            Name = "Jimi Hendrix",
            Birthday = new DateTime(1942, 11, 27)
        };

        await messagingManager.SendMessage<PersonData>(payloadPerson, "guitarplayers");

        // Test Request-Response 1
        var payloadPersonRequest = new PersonDataRequest
        {
            PersonId = 4711
        };
        await messagingManager.SendMessageRequest<PersonDataRequest>(payloadPersonRequest, Configs.personExchangeName);

        // Test Request-Response 2
        var payloadAddressRequest = new AddressDataRequest
        {
            CityId = 815
        };
        await messagingManager.SendMessageRequest<AddressDataRequest>(payloadAddressRequest, Configs.cityExchangeName);

        // Test Request-Response 3 - without Request-Payload, just as a trigger
        await messagingManager.SendMessageRequest(Configs.triggerExchangeName);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

