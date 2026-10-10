namespace OnlineStore.Models;

/// <summary>
/// 购物车中的一行商品（会话级快照，非数据库实体）。
/// 名称/价格/图片在加入时快照；支持记录所选的颜色/规格变体。
/// </summary>
public class CartItem
{
    public int ProductId { get; set; }

    /// <summary>所选规格变体 Id；无变体时为 null。</summary>
    public int? SkuId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Color { get; set; }

    public string? Spec { get; set; }

    public decimal Price { get; set; }

    public int Quantity { get; set; } = 1;

    public string? ImageUrl { get; set; }

    /// <summary>加入时的库存快照，用于限制购买数量。</summary>
    public int Stock { get; set; }

    /// <summary>小计 = 单价 × 数量。</summary>
    public decimal LineTotal => Price * Quantity;
}
