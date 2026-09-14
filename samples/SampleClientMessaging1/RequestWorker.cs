using MQTTnet;
using MqttServices.Core.Client;
using MqttServices.Core.Common;
using MqttServices.Core.Services;
using SampleCommon;
using System.Threading.Tasks;

namespace SampleClientMessaging1;

public class RequestWorker : BackgroundService
{
    private readonly ILogger<RequestWorker> logger;
    private readonly IMessagingManager messagingManager;
    private readonly IMqttClientService mqttClientService;

    public RequestWorker(ILogger<RequestWorker> logger, IMessagingManager messagingManager, IMqttClientService mqttClientService)
    {
        this.logger = logger;
        this.messagingManager = messagingManager;
        this.mqttClientService = mqttClientService;
        this.messagingManager.ResponseReceived += MessagingManager_ResponseReceived;
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

        if (e.ExchangeName == Configs.triggerExchangeName)
        {
            Console.WriteLine($"Trigger-Acknowledge {e.MessageId} received!");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Do not call Connect() here. The reconnect/discovery background service will handle connection & discovery.
        await mqttClientService.PublishMessage(Configs.guitarPlayersExchangeName, "Hello from Requester!");

        await StartDemo(stoppingToken);
    }

    private async Task StartDemo(CancellationToken stoppingToken)
    {
        await Task.Delay(500, stoppingToken);

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

        while (!stoppingToken.IsCancellationRequested)
        {
            Console.WriteLine("Select an option:");
            Console.WriteLine("1. Send Simple Message");
            Console.WriteLine("2. Send Trigger Message");
            Console.WriteLine("3. Send Message Request");
            Console.WriteLine("4. Send Message Request for all GuitarPlayers");
            Console.WriteLine("5. Send Raw MQTT-Message");
            Console.WriteLine("6. Send String Message");
            Console.WriteLine("7. Exit");

            var input = Console.ReadLine();

            switch (input)
            {
                case "1":
                    await SendSimpleMessage(guitarPlayer1, guitarPlayer2);
                    break;
                case "2":
                    await SendTriggerMessage();
                    break;
                case "3":
                    await SendMessageRequest(guitarPlayer1, guitarPlayer2);
                    break;
                case "4":
                    await SendMessageRequestForAll();
                    break;
                case "5":
                    await SendMqttMessage();
                    break;
                case "6":
                    await SendSimpleMessageAsString(guitarPlayer1);
                    break;
                case "7":
                    return;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }

    private async Task SendMqttMessage()
    {
        Console.WriteLine();
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Send Raw MQTT-Message");
        Console.ResetColor();
        var guitarPlayer = new GuitarPlayer
        {
            Name = "Jimi Hendrix",
            BirthDate = new DateTime(1942, 11, 27)
        };
        await mqttClientService.PublishMessage("mqttservices/test", guitarPlayer);
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
    private async Task SendSimpleMessageAsString(GuitarPlayer guitarPlayer1)
    {
        Console.WriteLine();
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Simple SendMessage with string-Data");
        Console.ResetColor();

        string jsonString = System.Text.Json.JsonSerializer.Serialize(guitarPlayer1);
        await messagingManager.SendString(jsonString, Configs.guitarPlayersSendExchangeName);
        Console.WriteLine($"**** Message sent!");
    }

    private async Task SendTriggerMessage()
    {
        Console.WriteLine();
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Simple SendMessage without data (without Response, to trigger something)");
        Console.ResetColor();
        Guid messageId = await messagingManager.SendMessageRequest(Configs.triggerExchangeName);
        Console.WriteLine($"**** Trigger-Message {messageId} sent!");
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

    public override void Dispose()
    {
        base.Dispose();
        this.messagingManager.ResponseReceived -= MessagingManager_ResponseReceived;
    }
}

