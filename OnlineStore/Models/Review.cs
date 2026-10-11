using System.ComponentModel.DataAnnotations;

namespace OnlineStore.Models;

/// <summary>商品评价：星级 + 文字 + 时间，支持种子演示数据与登录用户提交。</summary>
public class Review
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    /// <summary>提交评价的用户 Id；种子数据为空。</summary>
    public string? UserId { get; set; }

    [Required(ErrorMessage = "请填写昵称")]
    [StringLength(50)]
    [Display(Name = "昵称")]
    public string UserName { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "评分需在 1~5 星之间")]
    [Display(Name = "评分")]
    public int Rating { get; set; } = 5;

    [StringLength(1000)]
    [Display(Name = "评价内容")]
    public string? Comment { get; set; }

    [StringLength(500)]
    [Display(Name = "图片")]
    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
