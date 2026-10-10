using System.ComponentModel.DataAnnotations;
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
    [Display(Name = "SKU")]
    public string? Sku { get; set; }

    [StringLength(500)]
    [Url(ErrorMessage = "图片地址格式不正确")]
    [Display(Name = "图片地址")]
    public string? ImageUrl { get; set; }

    [Display(Name = "上架")]
    public bool IsActive { get; set; } = true;

    [Required(ErrorMessage = "请选择分类")]
    [Display(Name = "分类")]
    public int CategoryId { get; set; }

    public IEnumerable<SelectListItem>? Categories { get; set; }
}
