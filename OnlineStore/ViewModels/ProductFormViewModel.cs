using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OnlineStore.ViewModels;

public class ProductFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "ProductNameRequired")]
    [StringLength(150)]
    [Display(Name = "商品名称")]
    public string Name { get; set; } = string.Empty;

    [StringLength(150)]
    [Display(Name = "商品名称（英文）")]
    public string? NameEn { get; set; }

    [StringLength(2000)]
    [Display(Name = "商品描述")]
    public string? Description { get; set; }

    [StringLength(2000)]
    [Display(Name = "商品描述（英文）")]
    public string? DescriptionEn { get; set; }

    [Required(ErrorMessage = "PriceRequired")]
    [Range(0, 9999999, ErrorMessage = "PriceInvalid")]
    [Display(Name = "价格")]
    public decimal Price { get; set; }

    [Range(0, 9999999, ErrorMessage = "OriginalPriceInvalid")]
    [Display(Name = "原价")]
    public decimal? OriginalPrice { get; set; }

    [Required(ErrorMessage = "StockRequired")]
    [Range(0, int.MaxValue, ErrorMessage = "StockInvalid")]
    [Display(Name = "库存")]
    public int Stock { get; set; }

    [StringLength(100)]
    [Display(Name = "SKU 编码")]
    public string? Sku { get; set; }

    [StringLength(500)]
    [Display(Name = "图片地址")]
    public string? ImageUrl { get; set; }

    [Display(Name = "上传图片")]
    public IFormFile? ImageFile { get; set; }

    [Display(Name = "上架")]
    public bool IsActive { get; set; } = true;

    [Required(ErrorMessage = "ShopRequired")]
    [Display(Name = "店铺")]
    public int ShopId { get; set; }

    [MinLength(1, ErrorMessage = "CategoryRequired")]
    [Display(Name = "分类")]
    public List<int> CategoryIds { get; set; } = new();

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

    [Range(0, 9999999, ErrorMessage = "ShippingFeeAmountInvalid")]
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

    public IEnumerable<SelectListItem>? Shops { get; set; }

    public IEnumerable<SelectListItem>? Categories { get; set; }
}
