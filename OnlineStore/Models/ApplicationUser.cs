using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace OnlineStore.Models;

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

    public ICollection<DeliveryAddress> DeliveryAddresses { get; set; } = new List<DeliveryAddress>();

    [PersonalData]
    [StringLength(500)]
    [Display(Name = "头像")]
    public string? AvatarUrl { get; set; }
}
