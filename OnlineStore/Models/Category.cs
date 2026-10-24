using System.ComponentModel.DataAnnotations;

namespace OnlineStore.Models;

/// <summary>商品分类。</summary>
public class Category
{
    public int Id { get; set; }

    [Required(ErrorMessage = "CategoryNameRequired")]
    [StringLength(100)]
    [Display(Name = "分类名称")]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "分类名称（英文）")]
    public string? NameEn { get; set; }

    [StringLength(100)]
    public string Slug { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "分类描述")]
    public string? Description { get; set; }

    [StringLength(500)]
    [Display(Name = "分类描述（英文）")]
    public string? DescriptionEn { get; set; }

    [Display(Name = "排序")]
    public int DisplayOrder { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
