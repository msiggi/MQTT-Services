using MqttServices.Core.Common;
using MqttServices.Core.Services;
using SampleCommon;
using System.Numerics;

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
            GuitarPlayer player = (GuitarPlayer)e.Value;
            logger.LogInformation($"**** Message Received with GuitarPlayer {player.Name}!");

        }
    }

    private void MessagingManager_RequestReceived(object? sender, Payload e)
    {
        if (e.ExchangeName == Configs.GuitarPlayersExchangeName)
        {
            var player = (GuitarPlayer)e.Value;
            logger.LogInformation($"**** Message Received with GuitarPlayer {player.Name}, add Guitar and send it back!");

            player.OwnedGuitars = new List<Guitar> { new Guitar { Model = "Stratocaster", Brand = "Fender", Color = "White" } };
            messagingManager.SendMessageResponse<GuitarPlayer>(player, e.ExchangeName);
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
        Console.Title = "Responder";
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
