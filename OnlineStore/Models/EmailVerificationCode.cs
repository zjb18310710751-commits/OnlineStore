using System.ComponentModel.DataAnnotations;

namespace OnlineStore.Models;

/// <summary>邮箱验证码：注册时发送的 6 位验证码，带过期时间。</summary>
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
