using System.ComponentModel.DataAnnotations;

namespace OnlineStore.Models;

/// <summary>店铺：多个商品归属于同一店铺，展示店名、评分与销量。</summary>
public class Shop
{
    public int Id { get; set; }

    [Required(ErrorMessage = "请输入店铺名称")]
    [StringLength(100)]
    [Display(Name = "店铺名称")]
    public string Name { get; set; } = string.Empty;

    [Range(0, 5, ErrorMessage = "评分需在 0~5 之间")]
    [Display(Name = "店铺评分")]
    public decimal Rating { get; set; }

    [Display(Name = "累计销量")]
    public int SalesCount { get; set; }

    [StringLength(500)]
    [Display(Name = "店铺简介")]
    public string? Description { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
