using MqttServices.Core.Services;

namespace SampleClientMessaging1.Services;

public class ApplicationService1
{
    private readonly IMessagingManager messagingManager;

    public ApplicationService1(IMessagingManager messagingManager)
    {
        this.messagingManager = messagingManager;
        this.messagingManager.SendMessage(new { Name = "Test1" }, "testExchange");
    }
}
