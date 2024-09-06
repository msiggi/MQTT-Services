using MqttServices.Core.Services;

namespace SampleClientMessaging1.Services;

public class ApplicationService2
{
    private readonly IMessagingManager messagingManager;

    public ApplicationService2(IMessagingManager messagingManager)
    {
        this.messagingManager = messagingManager;
        this.messagingManager.SendMessage(new { Name = "Test2" }, "testExchange");
    }
}
