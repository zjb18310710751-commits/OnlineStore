namespace OnlineStore.Models;

/// <summary>
/// 购物车中的一行商品（会话级快照，非数据库实体）。
/// 名称/价格/图片在加入时快照，避免展示购物车时反复查询数据库。
/// </summary>
public class CartItem
{
    public int ProductId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Quantity { get; set; } = 1;

    public string? ImageUrl { get; set; }

    /// <summary>商品当前库存，用于限制购买数量并提示上限。</summary>
    public int Stock { get; set; }

    /// <summary>小计 = 单价 × 数量。</summary>
    public decimal LineTotal => Price * Quantity;
}
