using System.ComponentModel.DataAnnotations;

namespace OnlineStore.Models;

public class ProductImage
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    [StringLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
