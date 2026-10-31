using System.ComponentModel.DataAnnotations;

namespace OnlineStore.Models;

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

    [ConcurrencyCheck]
    public int FailedAttempts { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
