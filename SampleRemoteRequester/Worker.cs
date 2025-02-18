using MqttServices.Core.Services;
using SampleCommon;
using System;

namespace SampleRemoteRequester;

public class Worker : IHostedService
{
    private readonly ILogger<Worker> logger;
    private readonly IRemoteCallRequester<PersonData> remoteServiceRequester;

    public Worker(ILogger<Worker> logger, IRemoteCallRequester<PersonData> remoteServiceRequester)
    {
        this.logger = logger;
        this.remoteServiceRequester = remoteServiceRequester;
        this.remoteServiceRequester.AllItemsResponseReceived += RemoteServiceRequester_AllItemsResponseReceived;
    }

    private void RemoteServiceRequester_AllItemsResponseReceived(object? sender, List<PersonData> e)
    {
        foreach (var person in e)
        {
            logger.LogInformation($"{person.Name} received!");
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Worker running at: {time}", DateTime.Now);

        Console.BackgroundColor = ConsoleColor.White;
        Console.BackgroundColor = ConsoleColor.Red;
        Console.Title = "Samples - MQTT-Servcices";
        Console.WriteLine("----------------------------------------------");
        Console.WriteLine("-         MQTT-Services!        -");
        Console.WriteLine("----------------------------------------------");
        Console.ResetColor();
        Console.WriteLine();

        Console.WriteLine($"1 - Request all Persons");

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("Please choose sample class (press Enter to continue): ");
        Console.ResetColor();

        string? input = Console.ReadLine();

        switch (input)
        {
            case "1":
                await remoteServiceRequester.RequestAllItems();
                break;
            default:
                break;
        }

    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
