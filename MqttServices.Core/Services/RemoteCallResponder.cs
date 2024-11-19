using Microsoft.Extensions.Logging;
using MqttServices.Core.Common;
using Optilog.Common.Services.Configurations;

namespace MqttServices.Core.Services;

public class RemoteCallResponder<T> : IRemoteCallResponder<T> where T : class
{
    private readonly ILogger<RemoteCallResponder<T>> logger;
    private readonly IMessagingManager messagingManager;
    private readonly string exchangeTopicPrefix;

    public event EventHandler<T> UpsertItemRequestReceived;
    public event EventHandler<T> DeleteItemRequestReceived;
    public event EventHandler<int> GetOneItemRequestReceived;
    public event EventHandler<RequestFilter> GetAllItemsRequestReceived;
    public event EventHandler<List<T>> ItemsReceived;
    public event EventHandler<T> ItemReceived;

    public RemoteCallResponder(ILogger<RemoteCallResponder<T>> logger, IMessagingManager messagingManager, string exchangeTopicPrefix = "mqttservice")
    {
        this.logger = logger;
        this.messagingManager = messagingManager;
        this.exchangeTopicPrefix = exchangeTopicPrefix;
        this.messagingManager.RequestReceived += MessagingManager_RequestReceived;
    }
    private void MessagingManager_RequestReceived(object? sender, Payload e)
    {
        if (e.ExchangeName == GetExchangeName(ExchangeNames.GenericGetAll))
        {
            logger.LogInformation($"Received request for all items of type {typeof(T).Name}");
            GetAllItemsRequestReceived?.Invoke(this, (RequestFilter)e.Value);
        }
        if (e.ExchangeName == GetExchangeName(ExchangeNames.GenericGetOne))
        {
            logger.LogInformation($"Received request for one item of type {typeof(T).Name}");
            GetOneItemRequestReceived?.Invoke(this, (int)e.Value);
        }
        if (e.ExchangeName == GetExchangeName(ExchangeNames.GenericUpsert))
        {
            logger.LogInformation($"Received request for upsert item of type {typeof(T).Name}");
            UpsertItemRequestReceived?.Invoke(this, e.Value as T);
        }
        if (e.ExchangeName == GetExchangeName(ExchangeNames.GenericDelete))
        {
            logger.LogInformation($"Received request for delete item of type {typeof(T).Name}");
            DeleteItemRequestReceived?.Invoke(this, e.Value as T);
        }
        if (e.ExchangeName == GetExchangeName(ExchangeNames.GenericItemList))
        {
            logger.LogInformation($"Received List of {(e.Value as List<T>).Count} Items of type {typeof(T).Name}");
            ItemsReceived?.Invoke(this, e.Value as List<T>);
        }
        if (e.ExchangeName == GetExchangeName(ExchangeNames.GenericItem))
        {
            logger.LogInformation($"Received Item if type {typeof(T).Name}");
            ItemReceived?.Invoke(this, e.Value as T);
        }
    }
    
    public async Task SendAllItems(List<T> items)
    {
        logger.LogInformation($"Sending {items.Count} items of type {typeof(T).Name}");
        await messagingManager.SendMessageResponse(items, GetExchangeName(ExchangeNames.GenericGetAll));
    }
    public async Task SendOneItem(T item)
    {
        logger.LogInformation($"Sending one item of type {typeof(T).Name}");
        await messagingManager.SendMessageResponse(item, GetExchangeName(ExchangeNames.GenericGetOne));
    }
    public async Task SendUpsertedItem(T item)
    {
        logger.LogInformation($"Sending upserted item of type {typeof(T).Name}");
        await messagingManager.SendMessageResponse(item, GetExchangeName(ExchangeNames.GenericUpsert));
    }
    public async Task SendDeletedResponse(bool result)
    {
        logger.LogInformation($"Sending deleted response of type {typeof(T).Name}");
        await messagingManager.SendMessageResponse(result, GetExchangeName(ExchangeNames.GenericDelete));
    }
    private string GetExchangeName(string methodSpecificTopic)
    {
        return string.Concat(exchangeTopicPrefix, methodSpecificTopic, typeof(T).Name);
    }
}