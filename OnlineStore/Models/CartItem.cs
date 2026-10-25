namespace OnlineStore.Models;

public class CartItem
{
    public int ProductId { get; set; }

    public int? SkuId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? NameEn { get; set; }

    public string? Color { get; set; }

    public string? ColorEn { get; set; }

    public string? Spec { get; set; }

    public string? SpecEn { get; set; }

    public decimal Price { get; set; }

    public int Quantity { get; set; } = 1;

    public string? ImageUrl { get; set; }

    public int Stock { get; set; }

    public decimal LineTotal => Price * Quantity;
}
