using System.ComponentModel.DataAnnotations;

namespace OnlineStore.Models;

public class ProductSpec
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    [Required(ErrorMessage = "ParamNameRequired")]
    [StringLength(100)]
    [Display(Name = "参数名")]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "参数名（英文）")]
    public string? NameEn { get; set; }

    [Required(ErrorMessage = "ParamValueRequired")]
    [StringLength(200)]
    [Display(Name = "参数值")]
    public string Value { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "参数值（英文）")]
    public string? ValueEn { get; set; }

    [Display(Name = "排序")]
    public int DisplayOrder { get; set; }
}
