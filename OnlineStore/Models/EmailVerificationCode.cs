using System.ComponentModel.DataAnnotations;

namespace OnlineStore.Models;

/// <summary>验证码：注册/找回密码时发送的 6 位验证码，带过期时间。Email 列同时承载邮箱或手机号（目标地址），避免 EnsureCreated 下改表结构。</summary>
public class EmailVerificationCode
{
    public int Id { get; set; }

    [Required]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(6)]
    public string Code { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
