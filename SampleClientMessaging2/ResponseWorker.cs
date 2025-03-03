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
        this.messagingManager.RequestAllReceived += MessagingManager_RequestAllReceived;
        this.messagingManager.MessageReceived += MessagingManager_MessageReceived;
    }

    private async void MessagingManager_RequestAllReceived(object? sender, Payload e)
    {
        if (e.ExchangeName == Configs.guitarPlayersExchangeName)
        {
            Console.WriteLine($"**** Request Received for all GuitarPlayers, send them back!");
            var players = new List<GuitarPlayer>
                {
                    new GuitarPlayer { Name = "Jimi Hendrix", OwnedGuitars = new List<Guitar> { new Guitar { Model = "Stratocaster", Brand = "Fender", Color = "Sunburst" } } },
                    new GuitarPlayer { Name = "Eric Clapton", OwnedGuitars = new List<Guitar> { new Guitar { Model = "Stratocaster", Brand = "Fender", Color = "Black" } } },
                    new GuitarPlayer { Name = "Jimmy Page", OwnedGuitars = new List<Guitar> { new Guitar { Model = "Les Paul", Brand = "Gibson", Color = "Sunburst" } } },
                    new GuitarPlayer { Name = "David Gilmour", OwnedGuitars = new List<Guitar> { new Guitar { Model = "Stratocaster", Brand = "Fender", Color = "Black" } } }
                };
            e.SetValue(players);
            await messagingManager.SendMessageResponse(e);
        }
    }

    private void MessagingManager_MessageReceived(object? sender, Payload e)
    {
        if (e.ExchangeName == Configs.guitarPlayersSendExchangeName)
        {
            GuitarPlayer player = (GuitarPlayer)e.Value;
            Console.WriteLine($"**** Message Received with GuitarPlayer {player.Name}!");
        }
        if (e.ExchangeName == typeof(GuitarPlayer).Name)
        {
            GuitarPlayer player = (GuitarPlayer)e.Value;
            Console.WriteLine($"**** Message Received with GuitarPlayer {player.Name} (using Default Exchange Name)!");
        }
    }

    private async void MessagingManager_RequestReceived(object? sender, Payload e)
    {
        if (e.ExchangeName == Configs.guitarPlayersExchangeName)
        {
            if (e.RequestType == RequestType.Generic)
            {
                var player = (GuitarPlayer)e.Value;
                Console.WriteLine($"**** Message Received with GuitarPlayer {player.Name}, add Guitar and send it back!");

                player.OwnedGuitars = new List<Guitar> { new Guitar { Model = "Stratocaster", Brand = "Fender", Color = "White" } };
                e.SetValue(player);
            }
            await messagingManager.SendMessageResponse(e);
        }

        if (e.ExchangeName == Configs.triggerExchangeName)
        {
            Console.WriteLine($"**** Trigger-Request without Payload Received!");
            // Do something with this!
        }

        if (e.ExchangeName == typeof(GuitarPlayer).Name)
        {
            var player = (GuitarPlayer)e.Value;
            Console.WriteLine($"**** Message Received with GuitarPlayer {player.Name}, add Guitar and send it back!");

            player.OwnedGuitars = new List<Guitar> { new Guitar { Model = "Stratocaster", Brand = "Fender", Color = "Sunburst" } };
            e.SetValue(player);

            await messagingManager.SendMessageResponse(e);
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
