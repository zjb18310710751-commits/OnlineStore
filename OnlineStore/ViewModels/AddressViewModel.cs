namespace OnlineStore.ViewModels;

/// <summary>个人中心地址维护。</summary>
public class AddressViewModel : ProfileSidebarViewModel
{
    public int? Id { get; set; }

    public string? RecipientName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public string? AddressLine2 { get; set; }

    public string? City { get; set; }

    public string? PostalCode { get; set; }

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
