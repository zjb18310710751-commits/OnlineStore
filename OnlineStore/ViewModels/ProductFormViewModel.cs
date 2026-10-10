using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OnlineStore.ViewModels;

/// <summary>商品创建/编辑表单视图模型。</summary>
public class ProductFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "请输入商品名称")]
    [StringLength(150)]
    [Display(Name = "商品名称")]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    [Display(Name = "商品描述")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "请输入价格")]
    [Range(0, 9999999, ErrorMessage = "价格不能为负")]
    [Display(Name = "价格")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "请输入库存")]
    [Range(0, int.MaxValue, ErrorMessage = "库存不能为负")]
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

    [Required(ErrorMessage = "请选择店铺")]
    [Display(Name = "店铺")]
    public int ShopId { get; set; }

    [MinLength(1, ErrorMessage = "请至少选择一个分类")]
    [Display(Name = "分类")]
    public List<int> CategoryIds { get; set; } = new();

    [StringLength(100)]
    [Display(Name = "发货地")]
    public string? ShippingFrom { get; set; }

    [StringLength(50)]
    [Display(Name = "邮费")]
    public string? ShippingFee { get; set; }

    [StringLength(100)]
    [Display(Name = "发货时效")]
    public string? DeliveryTime { get; set; }

    public IEnumerable<SelectListItem>? Shops { get; set; }

    public IEnumerable<SelectListItem>? Categories { get; set; }
}
