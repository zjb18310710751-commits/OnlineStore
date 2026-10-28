namespace OnlineStore.ViewModels;

public class ChangePasswordViewModel : ProfileSidebarViewModel
{
    public string? RegisteredEmail { get; set; }

    public string? VerificationCode { get; set; }

    public string? NewPassword { get; set; }

    public string? ConfirmPassword { get; set; }
}
