using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace OnlineStore.Models;

/// <summary>
/// 应用用户（基于 ASP.NET Core Identity）。
/// 在 IdentityUser 基础上扩展姓名与地址；电话号码复用 IdentityUser 自带的 PhoneNumber。
/// </summary>
public class ApplicationUser : IdentityUser
{
    [PersonalData]
    [StringLength(100)]
    [Display(Name = "名")]
    public string FirstName { get; set; } = string.Empty;

    [PersonalData]
    [StringLength(100)]
    [Display(Name = "姓")]
    public string LastName { get; set; } = string.Empty;

    [PersonalData]
    [StringLength(500)]
    [Display(Name = "地址")]
    public string? Address { get; set; }
}
