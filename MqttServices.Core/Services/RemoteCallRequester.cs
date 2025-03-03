//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using MqttServices.Core.Client;
//using MqttServices.Core.Common;
//using Optilog.Common.Services.Configurations;
//using System.Data.SqlTypes;

//namespace MqttServices.Core.Services;

//public class RemoteCallRequester<T> : IRemoteCallRequester<T> where T : class
//{
//    public event EventHandler<List<T>> AllItemsResponseReceived;
//    public event EventHandler<T> OneItemResponseReceived;
//    public event EventHandler<T> UpsertItemResponseReceived;
//    public event EventHandler<bool> DeleteItemResponseReceived;

//    private readonly ILogger<RemoteCallRequester<T>> logger;
//    private readonly IMessagingManager messagingManager;
//    private readonly string exchangeTopicPrefix;

//    public RemoteCallRequester(ILogger<RemoteCallRequester<T>> logger, IMessagingManager messagingManager, string exchangeTopicPrefix = "mqttservice")
//    {
//        this.logger = logger;
//        this.messagingManager = messagingManager;
//        this.exchangeTopicPrefix = exchangeTopicPrefix;
//        this.messagingManager.ResponseReceived += MessagingManager_ResponseReceived;
//    }

//    private void MessagingManager_ResponseReceived(object? sender, Payload e)
//    {
//        if (e is null)
//            return;

//        if (e.Value is null)
//        {
//            return;
//        }
//        if (e.ExchangeName == GetExchangeName(ExchangeNames.GenericGetAll))
//        {
//            logger.LogInformation($"Received all items of type {typeof(T).Name}");
//            AllItemsResponseReceived?.Invoke(this, e.Value as List<T>);
//        }
//        if (e.ExchangeName == GetExchangeName(ExchangeNames.GenericGetOne))
//        {
//            logger.LogInformation($"Received one item of type {typeof(T).Name}");
//            OneItemResponseReceived?.Invoke(this, e.Value as T);
//        }
//        if (e.ExchangeName == GetExchangeName(ExchangeNames.GenericUpsert))
//        {
//            logger.LogInformation($"Received upserted item of type {typeof(T).Name}");
//            UpsertItemResponseReceived?.Invoke(this, e.Value as T);
//        }
//        if (e.ExchangeName == GetExchangeName(ExchangeNames.GenericDelete))
//        {
//            logger.LogInformation($"Received delete response of type {typeof(T).Name}");
//            DeleteItemResponseReceived?.Invoke(this, (bool)e.Value);
//        }
//    }

//    private string GetExchangeName(string methodSpecificTopic)
//    {
//        return string.Concat(exchangeTopicPrefix, methodSpecificTopic, typeof(T).Name);
//    }

//    public async Task RequestAllItems(string filter = null, string includeExpression = null)
//    {
//        RequestFilter requestFilter = new RequestFilter(filter, includeExpression);
//        await messagingManager.SendMessageRequest(requestFilter, GetExchangeName(ExchangeNames.GenericGetAll));
//    }
//    public async Task RequestOneItem(int id)
//    {
//        await messagingManager.SendMessageRequest(id, GetExchangeName(ExchangeNames.GenericGetOne));
//    }
//    public async Task RequestItemUpsert(T item)
//    {
//        await messagingManager.SendMessageRequest(item, GetExchangeName(ExchangeNames.GenericUpsert));
//    }
//    public async Task SendItems(List<T> items)
//    {
//        await messagingManager.SendMessageRequest(items, GetExchangeName(ExchangeNames.GenericItemList));
//    }
//    public async Task SendItem(T item)
//    {
//        await messagingManager.SendMessageRequest(item, GetExchangeName(ExchangeNames.GenericItem));    
//    }
//    public async Task RequestDeleteItem(T item)
//    {
//        await messagingManager.SendMessageRequest(item, GetExchangeName(ExchangeNames.GenericGetOne));
//    }
//}
