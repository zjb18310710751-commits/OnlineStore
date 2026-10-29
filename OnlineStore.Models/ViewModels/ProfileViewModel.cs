namespace OnlineStore.ViewModels;

public class ProfileSidebarViewModel
{
    public string? UserName { get; set; }

    public string? Email { get; set; }

    public string? DisplayName { get; set; }

    public string? AvatarUrl { get; set; }

    public string? ActiveSection { get; set; }
}

public class ProfileViewModel : ProfileSidebarViewModel
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? PhoneNumber { get; set; }
}
