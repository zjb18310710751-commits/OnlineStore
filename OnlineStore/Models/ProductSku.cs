using System.ComponentModel.DataAnnotations;

namespace OnlineStore.Models;

/// <summary>商品规格变体（SKU）：颜色 × 规格 两维，每个组合有独立价格、库存与图片。</summary>
public class ProductSku
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    [Required(ErrorMessage = "ColorRequired")]
    [StringLength(50)]
    [Display(Name = "颜色")]
    public string Color { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "颜色（英文）")]
    public string? ColorEn { get; set; }

    [Required(ErrorMessage = "SpecRequired")]
    [StringLength(50)]
    [Display(Name = "规格")]
    public string Spec { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "规格（英文）")]
    public string? SpecEn { get; set; }

    [Range(0, 9999999, ErrorMessage = "PriceInvalid")]
    [Display(Name = "价格")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "StockInvalid")]
    [Display(Name = "库存")]
    public int Stock { get; set; }

    [StringLength(500)]
    [Display(Name = "图片地址")]
    public string? ImageUrl { get; set; }

    [Display(Name = "启用")]
    public bool IsActive { get; set; } = true;
}
