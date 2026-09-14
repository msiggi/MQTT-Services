using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MqttServices.Core.Client;
using MqttServices.Core.Services;
using System.Timers;

namespace SampleWorkerService_Client;

public class LifeBeatService : ILifeBeatService, IHostedService, IDisposable
{
    public event EventHandler<LifeBeatInfo> LifeBeatReceived;

    private readonly ILogger<LifeBeatService> logger;
    private readonly IMessagingManager messagingManager;
    private readonly IMqttClientService mqttClientService;
    private LifeBeatInfo lifeBeatInfo;
    private System.Timers.Timer timer;
    private string topic = "Lifebeat_Exchange";
    private bool isDisposed;
    private bool isStarted;

    public LifeBeatService(ILogger<LifeBeatService> logger, IMessagingManager messagingManager, IMqttClientService mqttClientService)
    {
        this.logger = logger;
        this.logger.LogInformation("Initializing Lifebeat-Service");

        this.messagingManager = messagingManager;
        this.messagingManager.MessageReceived += MessagingManager_MessageReceived;
        this.mqttClientService = mqttClientService;

        lifeBeatInfo = new LifeBeatInfo
        {
            Name = System.Reflection.Assembly.GetEntryAssembly().GetName().Name,
            Version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString(),
            Timestamp = DateTime.UtcNow,
            StartupDate = DateTime.UtcNow,
            RunningHost = System.Net.Dns.GetHostName(),
        };

        timer = new System.Timers.Timer(10000);
        timer.Elapsed += TimerElapsed;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("LifeBeatService starting...");
        isStarted = true;
        timer.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("LifeBeatService stopping...");
        timer?.Stop();
        return Task.CompletedTask;
    }

    private void MessagingManager_MessageReceived(object? sender, MqttServices.Core.Common.Payload e)
    {
        if (e.ExchangeName == typeof(LifeBeatInfo).Name)
        {
            LifeBeatReceived?.Invoke(this, (LifeBeatInfo)e.Value);
        }
    }

    private void TimerElapsed(object? sender, ElapsedEventArgs e)
    {
        _ = Task.Run(SendLifeBeatMessage);
    }

    private async Task SendLifeBeatMessage()
    {
        if (!isStarted || !mqttClientService.IsConnected)
        {
            logger.LogDebug("Skipping lifebeat message - service not started or MQTT not connected");
            return;
        }

        lifeBeatInfo.Timestamp = DateTime.UtcNow;
        await messagingManager.SendMessage(lifeBeatInfo);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!isDisposed)
        {
            if (disposing)
            {
                timer.Dispose();
            }
            isDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}