
using MqttServices.Core.Services;
using SampleCommon;

namespace SampleRemoteResponder
{
    public class Worker : IHostedService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IRemoteCallResponder<PersonData> remoteCallResponder;

        public Worker(ILogger<Worker> logger, IRemoteCallResponder<PersonData> remoteCallResponder)
        {
            _logger = logger;
            this.remoteCallResponder = remoteCallResponder;
            this.remoteCallResponder.ItemReceived += RemoteCallResponder_ItemReceived; ;
        }

        private async void RemoteCallResponder_ItemReceived(object? sender, PersonData e)
        {
            e.OwnedCars = new List<OwnedCar> { new OwnedCar { Model = "Porsche" } };
            await remoteCallResponder.SendOneItem(e);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
