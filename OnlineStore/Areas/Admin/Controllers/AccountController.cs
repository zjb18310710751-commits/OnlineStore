using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using OnlineStore.Models;
using OnlineStore.Services;
using OnlineStore.ViewModels;

namespace OnlineStore.Areas.Admin.Controllers;

/// <summary>后台专属登录/登出：与前台用户登录分离，仅允许管理员角色登录。</summary>
[Area("Admin")]
public class AccountController : Controller
{
    private const string AdminRole = "Admin";

    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRememberedLoginService _rememberedLogin;
    private readonly IStringLocalizer<SharedResource> _loc;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IRememberedLoginService rememberedLogin,
        IStringLocalizer<SharedResource> localizer)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _rememberedLogin = rememberedLogin;
        _loc = localizer;
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
        return View(BuildLoginViewModel());
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
            ModelState.AddModelError(string.Empty, _loc["EmailOrPasswordIncorrect"].Value);
            return View(model);
        }

        // 后台登录的核心隔离：普通用户不具备管理员角色，直接拒绝
        if (!await _userManager.IsInRoleAsync(user, AdminRole))
        {
            ModelState.AddModelError(string.Empty, _loc["NotAdmin"].Value);
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            if (model.RememberCredentials)
            {
                _rememberedLogin.Save(model.Email, model.Password);
            }
            else
            {
                _rememberedLogin.Clear();
            }

            return RedirectToLocal(returnUrl) ?? RedirectToAction("Index", "Dashboard");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, _loc["AccountLocked"].Value);
            return View(model);
        }

        ModelState.AddModelError(string.Empty, _loc["EmailOrPasswordIncorrect"].Value);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    private LoginViewModel BuildLoginViewModel()
    {
        var remembered = _rememberedLogin.Load();
        if (remembered is null)
        {
            return new LoginViewModel();
        }

        return new LoginViewModel
        {
            Email = remembered.Email,
            Password = remembered.Password,
            RememberCredentials = true
        };
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
