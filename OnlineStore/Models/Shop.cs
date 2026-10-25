using System.ComponentModel.DataAnnotations;

namespace OnlineStore.Models;

public class Shop
{
    public int Id { get; set; }

    [Required(ErrorMessage = "ShopNameRequired")]
    [StringLength(100)]
    [Display(Name = "店铺名称")]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "店铺名称（英文）")]
    public string? NameEn { get; set; }

    [Range(0, 5, ErrorMessage = "ShopRatingInvalid")]
    [Display(Name = "店铺评分")]
    public decimal Rating { get; set; }

    [Display(Name = "累计销量")]
    public int SalesCount { get; set; }

    [StringLength(500)]
    [Display(Name = "店铺简介")]
    public string? Description { get; set; }

    [StringLength(500)]
    [Display(Name = "店铺简介（英文）")]
    public string? DescriptionEn { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
