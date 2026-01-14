
namespace SampleWorkerService_Client
{
    internal class LifeBeatInfoItem
    {
        public string? Name { get; internal set; }
        public string Version { get; internal set; }
        public DateTime Timestamp { get; internal set; }
        public DateTime StartupDate { get; internal set; }
        public string RunningHost { get; internal set; }
    }
}