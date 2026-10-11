using System.ComponentModel.DataAnnotations;

namespace OnlineStore.Models;

/// <summary>商品/产品。归属一个店铺，可属于多个分类，含规格变体、参数与评价。</summary>
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

    [Display(Name = "原价")]
    public decimal? OriginalPrice { get; set; }

    /// <summary>是否促销中（原价高于现价）。</summary>
    public bool IsOnSale => OriginalPrice.HasValue && OriginalPrice.Value > Price;

    /// <summary>折扣百分比（促销时），如 30 表示 -30%。</summary>
    public int? DiscountPercent =>
        IsOnSale ? (int)Math.Round((1m - Price / OriginalPrice!.Value) * 100m) : null;

    [Display(Name = "库存")]
    public int Stock { get; set; }

    [StringLength(100)]
    [Display(Name = "SKU 编码")]
    public string? Sku { get; set; }

    [StringLength(500)]
    [Display(Name = "图片地址")]
    public string? ImageUrl { get; set; }

    [Display(Name = "上架")]
    public bool IsActive { get; set; } = true;

    // 店铺
    public int ShopId { get; set; }

    public Shop? Shop { get; set; }

    // 多分类（多对多）
    public ICollection<Category> Categories { get; set; } = new List<Category>();

    // 快递信息
    [StringLength(100)]
    [Display(Name = "发货地")]
    public string? ShippingFrom { get; set; }

    [StringLength(50)]
    [Display(Name = "邮费")]
    public string? ShippingFee { get; set; }

    [StringLength(100)]
    [Display(Name = "发货时效")]
    public string? DeliveryTime { get; set; }

    [StringLength(10000)]
    [Display(Name = "图文介绍")]
    public string? DetailHtml { get; set; }

    // 规格变体 / 参数 / 评价
    public ICollection<ProductSku> Skus { get; set; } = new List<ProductSku>();

    public ICollection<ProductSpec> Specs { get; set; } = new List<ProductSpec>();

    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
