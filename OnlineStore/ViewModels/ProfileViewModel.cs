namespace OnlineStore.ViewModels;

/// <summary>个人中心侧栏所需的只读账号信息。</summary>
public class ProfileSidebarViewModel
{
    public string? UserName { get; set; }

    public string? Email { get; set; }

    public string? DisplayName { get; set; }

    public string? ActiveSection { get; set; }
}

/// <summary>基本个人信息：用户名和邮箱只读，姓名与电话可修改。</summary>
public class ProfileViewModel : ProfileSidebarViewModel
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? PhoneNumber { get; set; }
}
