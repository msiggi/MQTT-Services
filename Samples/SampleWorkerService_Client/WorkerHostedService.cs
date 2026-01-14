using MqttServices.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleWorkerService_Client;

public class WorkerHostedService : IHostedService
{
    private readonly IMessagingManager messagingManager;

    public WorkerHostedService(IMessagingManager messagingManager)
    {
        this.messagingManager = messagingManager;
    }   
    public Task StartAsync(CancellationToken cancellationToken)
    {
        messagingManager.PublishMqttMessage("test/topic", "Hello from WorkerHostedService");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
