using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineStore.Models;

public class Product
{
    public int Id { get; set; }

    [Required(ErrorMessage = "ProductNameRequired")]
    [StringLength(150)]
    [Display(Name = "商品名称")]
    public string Name { get; set; } = string.Empty;

    [StringLength(150)]
    [Display(Name = "商品名称（英文）")]
    public string? NameEn { get; set; }

    [StringLength(150)]
    public string Slug { get; set; } = string.Empty;

    [StringLength(2000)]
    [Display(Name = "商品描述")]
    public string? Description { get; set; }

    [StringLength(2000)]
    [Display(Name = "商品描述（英文）")]
    public string? DescriptionEn { get; set; }

    [Display(Name = "价格")]
    public decimal Price { get; set; }

    [Display(Name = "原价")]
    public decimal? OriginalPrice { get; set; }

    public bool IsOnSale => OriginalPrice.HasValue && OriginalPrice.Value > Price;

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

    public int ShopId { get; set; }

    public Shop? Shop { get; set; }

    public ICollection<Category> Categories { get; set; } = new List<Category>();

    [StringLength(100)]
    [Display(Name = "发货地")]
    public string? ShippingFrom { get; set; }

    [StringLength(100)]
    [Display(Name = "发货地（英文）")]
    public string? ShippingFromEn { get; set; }

    [StringLength(50)]
    [Display(Name = "邮费")]
    public string? ShippingFee { get; set; }

    [StringLength(50)]
    [Display(Name = "邮费（英文）")]
    public string? ShippingFeeEn { get; set; }

    [Display(Name = "邮费金额")]
    public decimal ShippingFeeAmount { get; set; }

    [StringLength(100)]
    [Display(Name = "发货时效")]
    public string? DeliveryTime { get; set; }

    [StringLength(100)]
    [Display(Name = "发货时效（英文）")]
    public string? DeliveryTimeEn { get; set; }

    [StringLength(10000)]
    [Display(Name = "图文介绍")]
    public string? DetailHtml { get; set; }

    [StringLength(10000)]
    [Display(Name = "图文介绍（英文）")]
    public string? DetailHtmlEn { get; set; }

    public ICollection<ProductSku> Skus { get; set; } = new List<ProductSku>();

    public ICollection<ProductSpec> Specs { get; set; } = new List<ProductSpec>();

    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

    [NotMapped]
    public double? AverageRating { get; set; }

    [NotMapped]
    public int ReviewCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
