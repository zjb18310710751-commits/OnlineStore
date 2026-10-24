namespace OnlineStore.ViewModels;

/// <summary>个人中心地址维护。</summary>
public class AddressViewModel : ProfileSidebarViewModel
{
    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public string? AddressLine2 { get; set; }

    public string? City { get; set; }

    public string? PostalCode { get; set; }

    public string? Country { get; set; }
}
