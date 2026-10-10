namespace OnlineStore.Models;

/// <summary>购物车聚合：保存多行购物项并提供汇总数据。</summary>
public class Cart
{
    public List<CartItem> Items { get; set; } = new();

    /// <summary>商品总件数（各条目数量之和）。</summary>
    public int TotalItems => Items.Sum(i => i.Quantity);

    /// <summary>合计金额（各条目小计之和）。</summary>
    public decimal Total => Items.Sum(i => i.LineTotal);
}
