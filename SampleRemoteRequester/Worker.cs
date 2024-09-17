using MqttServices.Core.Services;
using SampleCommon;

namespace SampleRemoteRequester
{
    public class Worker : IHostedService
    {
        private readonly ILogger<Worker> logger;
        private readonly IRemoteCallRequester<PersonData> remoteServiceRequester;

        public Worker(ILogger<Worker> logger, IRemoteCallRequester<PersonData> remoteServiceRequester)
        {
            this.logger = logger;
            this.remoteServiceRequester = remoteServiceRequester;
            this.remoteServiceRequester.OneItemResponseReceived += RemoteServiceRequester_OneItemResponseReceived;
        }

        private void RemoteServiceRequester_OneItemResponseReceived(object? sender, PersonData e)
        {
            logger.LogInformation($"**** Response Received, Person {e.Name}!");
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Worker running at: {time}", DateTime.Now);
            await remoteServiceRequester.SendItem(new PersonData
            {
                Birthday = new DateTime(1942, 11, 27),
                Name = "Jimi Hendrix"
            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
