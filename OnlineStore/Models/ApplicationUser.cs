using Microsoft.AspNetCore.Identity;

namespace OnlineStore.Models;

/// <summary>
/// 应用用户（基于 ASP.NET Core Identity）。
/// 预留扩展点：后续可增加 DisplayName、购物车、订单等字段。
/// </summary>
public class ApplicationUser : IdentityUser
{
}
