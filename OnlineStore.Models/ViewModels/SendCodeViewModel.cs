using System.ComponentModel.DataAnnotations;

namespace OnlineStore.ViewModels;

public class SendCodeViewModel
{
    [Required(ErrorMessage = "AccountRequired")]
    public string Target { get; set; } = string.Empty;

    public string Channel { get; set; } = "email";

    public string Purpose { get; set; } = "register";
}
