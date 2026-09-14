
namespace SampleWorkerService_Client;

public interface ILifeBeatService
{
    event EventHandler<LifeBeatInfo> LifeBeatReceived;
}