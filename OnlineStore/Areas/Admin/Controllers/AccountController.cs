using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineStore.Models;
using OnlineStore.ViewModels;

namespace OnlineStore.Areas.Admin.Controllers;

/// <summary>后台专属登录/登出：与前台用户登录分离，仅允许管理员角色登录。</summary>
[Area("Admin")]
public class AccountController : Controller
{
    private const string AdminRole = "Admin";

    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return User.IsInRole(AdminRole)
                ? RedirectToAction("Index", "Dashboard")
                : RedirectToAction("Index", "Home", new { area = "" });
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            // 统一错误信息，避免泄露账号是否存在（安全性）
            ModelState.AddModelError(string.Empty, "邮箱或密码错误。");
            return View(model);
        }

        // 后台登录的核心隔离：普通用户不具备管理员角色，直接拒绝
        if (!await _userManager.IsInRoleAsync(user, AdminRole))
        {
            ModelState.AddModelError(string.Empty, "该账号无管理员权限。");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return RedirectToLocal(returnUrl) ?? RedirectToAction("Index", "Dashboard");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "登录失败次数过多，账号已锁定，请稍后再试。");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "邮箱或密码错误。");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    private IActionResult? RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return null;
    }
}
