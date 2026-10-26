using System.ComponentModel.DataAnnotations;

namespace OnlineStore.Models;

public class Review
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public int? OrderId { get; set; }

    public Order? Order { get; set; }

    public string? UserId { get; set; }

    [Required(ErrorMessage = "UserNameRequired")]
    [StringLength(50)]
    [Display(Name = "昵称")]
    public string UserName { get; set; } = string.Empty;

    [StringLength(50)]
    public string? UserNameEn { get; set; }

    [Range(1, 5, ErrorMessage = "RatingInvalid")]
    [Display(Name = "评分")]
    public int Rating { get; set; } = 5;

    [StringLength(1000)]
    [Display(Name = "评价内容")]
    public string? Comment { get; set; }

    [StringLength(1000)]
    public string? CommentEn { get; set; }

    [StringLength(500)]
    [Display(Name = "图片")]
    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
