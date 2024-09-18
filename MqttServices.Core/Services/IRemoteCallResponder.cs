using Optilog.Common.Services.Configurations;

namespace MqttServices.Core.Services
{
    public interface IRemoteCallResponder<T> where T : class
    {
        event EventHandler<T> DeleteItemRequestReceived;
        event EventHandler<RequestFilter> GetAllItemsRequestReceived;
        event EventHandler<int> GetOneItemRequestReceived;
        event EventHandler<T> UpsertItemRequestReceived;
        event EventHandler<List<T>> ItemsReceived;
        event EventHandler<T> ItemReceived;

        Task SendAllItems(List<T> items);
        Task SendDeletedResponse(bool result);
        Task SendOneItem(T item);
        Task SendUpsertedItem(T item);
    }
}