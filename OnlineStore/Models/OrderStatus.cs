namespace OnlineStore.Models;

/// <summary>订单状态常量。</summary>
public static class OrderStatus
{
    public const string PendingShipment = "待发货";
    public const string Shipped = "已发货";
    public const string Completed = "已完成";
    public const string Cancelled = "已取消";
    public const string Refunded = "已退款";
}
