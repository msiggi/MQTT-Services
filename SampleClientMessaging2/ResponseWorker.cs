using MqttServices.Core.Common;
using MqttServices.Core.Services;
using SampleCommon;

namespace SampleClientMessaging2;

public class ResponseWorker : IHostedService
{
    private readonly ILogger<ResponseWorker> logger;
    private readonly IMessagingManager messagingManager;

    public ResponseWorker(ILogger<ResponseWorker> logger, IMessagingManager messagingManager)
    {
        this.logger = logger;
        this.messagingManager = messagingManager;
        this.messagingManager.RequestReceived += MessagingManager_RequestReceived;
        this.messagingManager.MessageReceived += MessagingManager_MessageReceived;
    }

    private void MessagingManager_MessageReceived(object? sender, Payload e)
    {
        if (e.ExchangeName == "guitarplayers")
        {
            PersonData person = (PersonData)e.Value;
            logger.LogInformation($"**** Message Received, Person {person.Name}!");
        }
    }

    private void MessagingManager_RequestReceived(object? sender, Payload e)
    {
        if (e.ExchangeName == Configs.personExchangeName)
        {
            var person = (PersonDataRequest)e.Value;
            logger.LogInformation($"**** Request Received, Person {person.PersonId} requested, sending Answer...!");

            var personId = person.PersonId;

            // *****************************************
            // get complete Person per Id from elsewhere
            // .................
            // *****************************************

            PersonDataResponse personResponse = new PersonDataResponse
            {
                PersonData = new PersonData
                {
                    PersonId = personId,
                    Name = "Max Mustermann",
                    Birthday = new DateTime(1999, 4, 1)
                }
            };

            messagingManager.SendMessageResponse<PersonDataResponse>(personResponse, e.ExchangeName);
        }
        if (e.ExchangeName == Configs.cityExchangeName)
        {
            var city = (AddressDataRequest)e.Value;
            logger.LogInformation($"**** Request Received, City {city.CityId} requested, sending Answer...!");

            var cityId = city.CityId;

            // *****************************************
            // get complete Address per Id from elsewhere
            // .................
            // *****************************************

            AddressData address = new AddressData
            {
                CityId = cityId,
                CityName = "Dresden",
                Street = "Washingtonstraﬂe",
                Number = 16
            };

            messagingManager.SendMessageResponse<AddressData>(address, e.ExchangeName);
        }
        if (e.ExchangeName == Configs.triggerExchangeName)
        {
            logger.LogInformation($"**** Trigger-Request without Payload Received!");
            // Do something with this!
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
