using System.Text.Json;
using Microsoft.AspNetCore.Http;
using OnlineStore.Models;

namespace OnlineStore.Services;

/// <summary>基于 Session 的购物车存储：将购物车序列化为 JSON 存入会话。</summary>
public class SessionCartStore : ICartStore
{
    private const string SessionKey = "Cart";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpContextAccessor _httpContextAccessor;

    public SessionCartStore(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<List<CartItem>> GetAsync()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        var json = session?.GetString(SessionKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return Task.FromResult(new List<CartItem>());
        }

        var items = JsonSerializer.Deserialize<List<CartItem>>(json, JsonOptions);
        return Task.FromResult(items ?? new List<CartItem>());
    }

    public Task SaveAsync(List<CartItem> items)
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session is not null)
        {
            session.SetString(SessionKey, JsonSerializer.Serialize(items, JsonOptions));
        }

        return Task.CompletedTask;
    }
}
