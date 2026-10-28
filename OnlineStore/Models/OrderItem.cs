using System.ComponentModel.DataAnnotations;

namespace OnlineStore.Models;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public Order? Order { get; set; }

    public int ProductId { get; set; }

    public int? SkuId { get; set; }

    [StringLength(150)]
    public string ProductName { get; set; } = string.Empty;

    [StringLength(150)]
    public string? ProductNameEn { get; set; }

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    [StringLength(50)]
    public string? Color { get; set; }

    [StringLength(50)]
    public string? ColorEn { get; set; }

    [StringLength(50)]
    public string? Spec { get; set; }

    [StringLength(50)]
    public string? SpecEn { get; set; }

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal => Price * Quantity;
}
