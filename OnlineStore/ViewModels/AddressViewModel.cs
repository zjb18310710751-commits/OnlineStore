using System.ComponentModel.DataAnnotations;

namespace OnlineStore.ViewModels;

public class AddressViewModel : ProfileSidebarViewModel
{
    public int? Id { get; set; }

    [StringLength(100)]
    public string? RecipientName { get; set; }

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(200)]
    public string? AddressLine2 { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? PostalCode { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    public bool IsDefault { get; set; }

    public IReadOnlyList<DeliveryAddressItemViewModel> Addresses { get; set; } =
        Array.Empty<DeliveryAddressItemViewModel>();
}

public class DeliveryAddressItemViewModel
{
    public int Id { get; set; }

    public string RecipientName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string AddressLine1 { get; set; } = string.Empty;

    public string? AddressLine2 { get; set; }

    public string City { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public bool IsDefault { get; set; }
}
