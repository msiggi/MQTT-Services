using MqttServices.Core.Common;
using MqttServices.Core.Services;
using SampleClientMessaging1.Services;
using SampleCommon;

namespace SampleClientMessaging1;

public class RequestWorker : IHostedService
{
    private readonly ILogger<RequestWorker> logger;
    private readonly IMessagingManager messagingManager;

    public RequestWorker(ILogger<RequestWorker> logger, IMessagingManager messagingManager, ApplicationService1 applicationService1, ApplicationService2 applicationService2)
    {
        this.logger = logger;
        this.messagingManager = messagingManager;
        this.messagingManager.ResponseReceived += MessagingManager_ResponseReceived;
    }

    private void MessagingManager_ResponseReceived(object? sender, Payload e)
    {
        if (e.ExchangeName == Configs.GuitarPlayersExchangeName)
        {
            GuitarPlayer player = (GuitarPlayer)e.Value;
            logger.LogInformation($"**** Response Received with GuitarPlayer {player.Name} owned a {player.OwnedGuitars.FirstOrDefault().Color} {player.OwnedGuitars.FirstOrDefault().Brand} {player.OwnedGuitars.FirstOrDefault().Model}!");
        }
        if (e.ExchangeName == Configs.cityExchangeName)
        {
            logger.LogInformation($"**** Response Received with City {((AddressData)e.Value).CityName}!");
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _ = StartDemo();

        // Test Request-Response 1
        //var payloadPersonRequest = new PersonDataRequest
        //{
        //    PersonId = 4711
        //};
        //await messagingManager.SendMessageRequest<PersonDataRequest>(payloadPersonRequest, Configs.personExchangeName);

        //// Test Request-Response 2
        //var payloadAddressRequest = new AddressDataRequest
        //{
        //    CityId = 815
        //};
        //await messagingManager.SendMessageRequest<AddressDataRequest>(payloadAddressRequest, Configs.cityExchangeName);

        //// Test Request-Response 3 - without Request-Payload, just as a trigger
        //await messagingManager.SendMessageRequest(Configs.triggerExchangeName);
    }
    public async Task StartDemo()
    {
        await Task.Delay(1000);

        Console.Title = "Requester";
        Console.BackgroundColor = ConsoleColor.White;
        Console.BackgroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine();
        Console.WriteLine();

        Console.WriteLine("----------------------------------------------");
        Console.WriteLine("-                 MQTT-Services!             -");
        Console.WriteLine("----------------------------------------------");
        Console.ResetColor();
        Console.WriteLine();

        var guitarPlayer = new GuitarPlayer
        {
            Name = "Jimi Hendrix",
            BirthDate = new DateTime(1942, 11, 27)
        };

        Console.WriteLine("Press any key to send a simple message without expecting answer");
        Console.ReadKey();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("Simple SendMessage (without Response)");
        Console.ResetColor();
        await messagingManager.SendMessage<GuitarPlayer>(guitarPlayer, "guitarplayers");
        logger.LogInformation($"**** Message sent ({guitarPlayer.Name})!");

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("Press any key to send a message-Request expecting answer");
        Console.ReadKey();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("Simple SendMessage (without Response)");
        Console.ResetColor();
        await messagingManager.SendMessageRequest<GuitarPlayer>(guitarPlayer, Configs.GuitarPlayersExchangeName);
        logger.LogInformation($"**** Message sent ({guitarPlayer.Name})!");


    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

