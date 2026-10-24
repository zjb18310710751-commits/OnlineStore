using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace OnlineStore.Models;

/// <summary>用户保存的收货地址；每个用户可维护多条，其中最多一条为默认地址。</summary>
public class DeliveryAddress
{
    public int Id { get; set; }

    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [PersonalData]
    [Required]
    [StringLength(100)]
    public string RecipientName { get; set; } = string.Empty;

    [PersonalData]
    [Required]
    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [PersonalData]
    [Required]
    [StringLength(200)]
    public string AddressLine1 { get; set; } = string.Empty;

    [PersonalData]
    [StringLength(200)]
    public string? AddressLine2 { get; set; }

    [PersonalData]
    [Required]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [PersonalData]
    [Required]
    [StringLength(100)]
    public string PostalCode { get; set; } = string.Empty;

    [PersonalData]
    [Required]
    [StringLength(100)]
    public string Country { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
}
