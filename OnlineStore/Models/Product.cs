using System.ComponentModel.DataAnnotations;

namespace OnlineStore.Models;

/// <summary>商品/产品。</summary>
public class Product
{
    public int Id { get; set; }

    [Required(ErrorMessage = "请输入商品名称")]
    [StringLength(150)]
    [Display(Name = "商品名称")]
    public string Name { get; set; } = string.Empty;

    [StringLength(150)]
    public string Slug { get; set; } = string.Empty;

    [StringLength(2000)]
    [Display(Name = "商品描述")]
    public string? Description { get; set; }

    [Display(Name = "价格")]
    public decimal Price { get; set; }

    [Display(Name = "库存")]
    public int Stock { get; set; }

    [StringLength(100)]
    [Display(Name = "SKU")]
    public string? Sku { get; set; }

    [StringLength(500)]
    [Display(Name = "图片地址")]
    public string? ImageUrl { get; set; }

    [Display(Name = "上架")]
    public bool IsActive { get; set; } = true;

    public int CategoryId { get; set; }

    public Category? Category { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
