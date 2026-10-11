namespace OnlineStore.Models;

/// <summary>用户收藏（心愿单）：用户对商品的一对一收藏关系。</summary>
public class Favorite
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
