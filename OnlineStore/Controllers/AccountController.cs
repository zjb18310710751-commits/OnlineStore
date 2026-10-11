using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using OnlineStore.Models;
using OnlineStore.Services;
using OnlineStore.ViewModels;

namespace OnlineStore.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IVerificationCodeService _verificationCodeService;
    private readonly IStringLocalizer<SharedResource> _loc;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IVerificationCodeService verificationCodeService,
        IStringLocalizer<SharedResource> localizer)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _emailService = emailService;
        _verificationCodeService = verificationCodeService;
        _loc = localizer;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        // 后台地址的登录请求，引导到后台专属登录页（管理员/用户两套入口分离）
        if (!string.IsNullOrEmpty(returnUrl) &&
            returnUrl.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("Login", "Account", new { area = "Admin", returnUrl });
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

        var result = await _signInManager.PasswordSignInAsync(
            user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            // 前台登录面向普通用户，默认回到首页（不再跳后台）
            return RedirectToLocal(returnUrl) ?? RedirectToAction("Index", "Home");
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
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendCode([FromBody] SendCodeViewModel model)
    {
        if (model is null || !ModelState.IsValid)
        {
            return Json(new { ok = false, error = _loc["InvalidEmail"].Value });
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (model.Purpose == "reset")
        {
            // 找回密码：邮箱必须已注册
            if (user is null)
            {
                return Json(new { ok = false, error = _loc["EmailNotRegistered"].Value });
            }
        }
        else
        {
            // 注册：邮箱必须未注册
            if (user is not null)
            {
                return Json(new { ok = false, error = _loc["EmailAlreadyRegistered"].Value });
            }
        }

        try
        {
            var code = await _verificationCodeService.GenerateAsync(model.Email);
            var subject = _loc["VerificationEmailSubject"].Value;
            var body = EmailTemplateBuilder.BuildVerificationCode(code);
            await _emailService.SendAsync(model.Email, subject, body);
            return Json(new { ok = true });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { ok = false, error = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // 邮箱唯一性校验（统一提示，避免泄露账号是否已存在）
        if (await _userManager.FindByEmailAsync(model.Email) is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "该邮箱已被注册。");
            return View(model);
        }

        // 校验邮箱验证码
        if (!await _verificationCodeService.VerifyAsync(model.Email, model.VerificationCode))
        {
            ModelState.AddModelError(nameof(model.VerificationCode), _loc["InvalidVerificationCode"].Value);
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true, // 通过验证码校验即视为邮箱已验证
            FirstName = model.FirstName,
            LastName = model.LastName,
            Address = model.Address,
            PhoneNumber = model.PhoneNumber
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // 注册成功后自动登录并跳转首页
        await _signInManager.SignInAsync(user, isPersistent: false);
        TempData["Success"] = "注册成功，欢迎使用！";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            ModelState.AddModelError(nameof(model.Email), _loc["EmailNotRegistered"].Value);
            return View(model);
        }

        if (!await _verificationCodeService.VerifyAsync(model.Email, model.VerificationCode))
        {
            ModelState.AddModelError(nameof(model.VerificationCode), _loc["InvalidVerificationCode"].Value);
            return View(model);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        TempData["Success"] = _loc["PasswordResetSuccess"].Value;
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        return View(new ProfileViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            Address = user.Address ?? string.Empty
        });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel vm)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!ModelState.IsValid)
        {
            vm.Email = user.Email ?? string.Empty;
            return View(vm);
        }

        user.FirstName = vm.FirstName;
        user.LastName = vm.LastName;
        user.PhoneNumber = vm.PhoneNumber;
        user.Address = vm.Address;
        await _userManager.UpdateAsync(user);

        TempData["Success"] = "资料已保存。";
        return RedirectToAction(nameof(Profile));
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
