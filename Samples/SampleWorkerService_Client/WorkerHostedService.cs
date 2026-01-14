using MqttServices.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleWorkerService_Client;

public class WorkerHostedService : IHostedService, IDisposable
{
    private readonly IMessagingManager messagingManager;
    private readonly LifeBeatInfo lifeBeatInfo;
    private Timer? _timer;
    private int _messageCount = 0;

    public WorkerHostedService(IMessagingManager messagingManager)
    {
        this.messagingManager = messagingManager;
        lifeBeatInfo = new LifeBeatInfo
        {
            Name = System.Reflection.Assembly.GetEntryAssembly().GetName().Name,
            Version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString(),
            Timestamp = DateTime.UtcNow,
            StartupDate = DateTime.UtcNow,
            RunningHost = System.Net.Dns.GetHostName(),
        };
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Timer: alle 5 Sekunden ausführen
        _timer = new Timer(SendMessage, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

        return Task.CompletedTask;
    }

    private async void SendMessage(object? state)
    {
        _messageCount++;

        lifeBeatInfo.Timestamp = DateTime.UtcNow;
        var message = $"Hello from WorkerHostedService - Message #{_messageCount} at {DateTime.Now:HH:mm:ss}";
        Console.WriteLine($"Publishing message: {message}");
        await messagingManager.SendMessage(lifeBeatInfo);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
