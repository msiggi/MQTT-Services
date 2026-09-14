namespace MqttServices.Core.Services;

public interface IRemoteCallRequester<T> where T : class
{
    event EventHandler<List<T>> AllItemsResponseReceived;
    event EventHandler<bool> DeleteItemResponseReceived;
    event EventHandler<T> OneItemResponseReceived;
    event EventHandler<T> UpsertItemResponseReceived;

    Task RequestAllItems(string filter = null, string includeExpression = null);
    Task RequestDeleteItem(T item);
    Task RequestItemUpsert(T item);
    Task RequestOneItem(int id);
    Task SendItems(List<T> items);
    Task SendItem(T item);
}