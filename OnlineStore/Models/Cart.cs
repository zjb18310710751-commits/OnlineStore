namespace OnlineStore.Models;

public class Cart
{
    public List<CartItem> Items { get; set; } = new();

    public int TotalItems => Items.Sum(i => i.Quantity);

    public int ItemCount => Items.Count;

    public decimal Total => Items.Sum(i => i.LineTotal);

    public decimal ShippingTotal => Items
        .GroupBy(item => item.ProductId)
        .Sum(group => group.First().ShippingFeeAmount);

    public decimal GrandTotal => Total + ShippingTotal;
}
