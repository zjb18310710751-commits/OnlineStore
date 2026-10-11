using System.ComponentModel.DataAnnotations;

namespace OnlineStore.Models;

/// <summary>订单明细：下单时的商品快照（名称/颜色/规格/单价/数量）。</summary>
public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public Order? Order { get; set; }

    public int ProductId { get; set; }

    [StringLength(150)]
    public string ProductName { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Color { get; set; }

    [StringLength(50)]
    public string? Spec { get; set; }

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    /// <summary>小计 = 单价 × 数量。</summary>
    public decimal LineTotal => Price * Quantity;
}
