using MqttServices.Core.Common;
using MqttServices.Core.Services;
using SampleClientMessaging1.Services;
using SampleCommon;
using System.Threading.Tasks;

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
        this.messagingManager.MqttConnected += MessagingManager_MqttConnected;
    }

    private async Task MessagingManager_MqttConnected(object? sender, EventArgs e)
    {
        await StartDemo();
    }

    private void MessagingManager_ResponseReceived(object? sender, Payload e)
    {
        if (e.ExchangeName == Configs.guitarPlayersExchangeName)
        {
            if (e.RequestType == RequestType.GetAll)
            {
                var players = (List<GuitarPlayer>)e.Value;
                Console.WriteLine($"**** Response Received with {players.Count} GuitarPlayers!");
                foreach (var plr in players)
                {
                    Console.WriteLine($"   ** Response Received with GuitarPlayer {plr.Name} owned a {plr.OwnedGuitars.FirstOrDefault().Color} {plr.OwnedGuitars.FirstOrDefault().Brand} {plr.OwnedGuitars.FirstOrDefault().Model}!");
                }
                return;
            }
            GuitarPlayer player = (GuitarPlayer)e.Value;
            Console.WriteLine($"**** Response Received with GuitarPlayer {player.Name} owned a {player.OwnedGuitars.FirstOrDefault().Color} {player.OwnedGuitars.FirstOrDefault().Brand} {player.OwnedGuitars.FirstOrDefault().Model}!");
        }
        if (e.ExchangeName == typeof(GuitarPlayer).Name)
        {
            GuitarPlayer player = (GuitarPlayer)e.Value;
            Console.WriteLine($"**** Response Received with GuitarPlayer {player.Name} owned a {player.OwnedGuitars.FirstOrDefault().Color} {player.OwnedGuitars.FirstOrDefault().Brand} {player.OwnedGuitars.FirstOrDefault().Model} (using Default Exchange Name)!");
            if (e.MessageId != Guid.Empty)
            {
                Console.WriteLine($"MessageId: {e.MessageId}. Use it, to assign Response to Request");
            }
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _ = StartDemo();
    }

    public async Task StartDemo()
    {
        await Task.Delay(500);

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

        var guitarPlayer1 = new GuitarPlayer
        {
            Name = "Jimi Hendrix",
            BirthDate = new DateTime(1942, 11, 27)
        };

        var guitarPlayer2 = new GuitarPlayer
        {
            Name = "Eric Clapton",
            BirthDate = new DateTime(1945, 3, 30)
        };

        while (true)
        {
            Console.WriteLine("Select an option:");
            Console.WriteLine("1. Send Simple Message");
            Console.WriteLine("2. Send Trigger Message");
            Console.WriteLine("3. Send Message Request");
            Console.WriteLine("4. Send Message Request for all GuitarPlayers");
            Console.WriteLine("5. Exit");

            var input = Console.ReadLine();

            switch (input)
            {
                case "1":
                    await SendSimpleMessage(guitarPlayer1, guitarPlayer2);
                    await StartDemo();
                    break;
                case "2":
                    await SendTriggerMessage();
                    await StartDemo();
                    break;
                case "3":
                    await SendMessageRequest(guitarPlayer1, guitarPlayer2);
                    await StartDemo();
                    break;
                case "4":
                    await SendMessageRequestForAll();
                    await StartDemo();
                    break;
                case "5":
                    return;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }

    private async Task SendSimpleMessage(GuitarPlayer guitarPlayer1, GuitarPlayer guitarPlayer2)
    {
        Console.WriteLine();
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Simple SendMessage with data (without Response, testing default and specific exchange name))");
        Console.ResetColor();
        await messagingManager.SendMessage<GuitarPlayer>(guitarPlayer1, Configs.guitarPlayersSendExchangeName);
        Console.WriteLine($"**** Message sent ({guitarPlayer1.Name})!");
        await messagingManager.SendMessage(guitarPlayer2);
        Console.WriteLine($"**** Message sent ({guitarPlayer2.Name})!");
    }

    private async Task SendTriggerMessage()
    {
        Console.WriteLine();
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Simple SendMessage without data (without Response, to trigger something)");
        Console.ResetColor();
        await messagingManager.SendMessageRequest(Configs.triggerExchangeName);
        Console.WriteLine($"**** Trigger-Message sent!");
    }

    private async Task SendMessageRequest(GuitarPlayer guitarPlayer1, GuitarPlayer guitarPlayer2)
    {
        Console.WriteLine();
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Simple SendMessage with data awaiting Response");
        Console.ResetColor();
        await messagingManager.SendMessageRequest<GuitarPlayer>(guitarPlayer1, Configs.guitarPlayersExchangeName);
        Console.WriteLine($"**** Message sent ({guitarPlayer1.Name})!");
        await messagingManager.SendMessageRequest(guitarPlayer2);
        Console.WriteLine($"**** Message sent ({guitarPlayer2.Name})!");
    }
    private async Task SendMessageRequestForAll()
    {
        Console.WriteLine();
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Send Request Get all GuitarPlayers");
        Console.ResetColor();
        await messagingManager.SendMessageRequest(Configs.guitarPlayersExchangeName, RequestType.GetAll);
        Console.WriteLine($"**** {RequestType.GetAll} Request sent!");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

