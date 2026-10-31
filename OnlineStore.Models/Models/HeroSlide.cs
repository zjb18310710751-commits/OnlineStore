using System.ComponentModel.DataAnnotations;

namespace OnlineStore.Models;

public class HeroSlide
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [StringLength(100)]
    public string? TitleEn { get; set; }

    [Required, StringLength(120)]
    public string Heading { get; set; } = string.Empty;

    [StringLength(120)]
    public string? HeadingEn { get; set; }

    [StringLength(240)]
    public string? Description { get; set; }

    [StringLength(240)]
    public string? DescriptionEn { get; set; }

    [Required, StringLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [StringLength(200)]
    public string? ImageAlt { get; set; }

    [StringLength(200)]
    public string? ImageAltEn { get; set; }

    [Required, StringLength(500)]
    public string LinkUrl { get; set; } = "/Products";

    [Range(0, 9999)]
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
