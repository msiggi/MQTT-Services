namespace MqttServices.Core.Common;

public static partial class ExchangeNames
{

    #region generic
    public static string GenericGetOne { get; set; } = $"getone";
    public static string GenericGetAll { get; set; } = $"getall";
    public static string GenericUpsert { get; set; } = $"upsert";
    public static string GenericItemList { get; set; } = $"itemlist";
    public static string GenericItem { get; set; } = $"itemsingle";
    public static string GenericDelete { get; set; } = $"delete";
    #endregion

}
