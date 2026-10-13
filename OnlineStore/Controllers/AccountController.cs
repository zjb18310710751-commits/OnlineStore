using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
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
    private readonly IRememberedLoginService _rememberedLogin;
    private readonly IStringLocalizer<SharedResource> _loc;

    private const string PhonePattern = @"^[0-9+\-\s()]{6,20}$";

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IVerificationCodeService verificationCodeService,
        IRememberedLoginService rememberedLogin,
        IStringLocalizer<SharedResource> localizer)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _emailService = emailService;
        _verificationCodeService = verificationCodeService;
        _rememberedLogin = rememberedLogin;
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

        // 登录框支持邮箱或手机号：含 @ 视为邮箱，否则视为手机号（手机注册账号的 UserName 即手机号）
        var identifier = model.Email.Trim();
        var user = identifier.Contains('@')
            ? await _userManager.FindByEmailAsync(identifier)
            : await _userManager.FindByNameAsync(identifier);

        if (user is null)
        {
            // 统一错误信息，避免泄露账号是否存在（安全性）
            ModelState.AddModelError(string.Empty, "账号或密码错误。");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            // 勾选「记住账号密码」时加密保存凭据，否则清除旧凭据
            if (model.RememberCredentials)
            {
                _rememberedLogin.Save(model.Email, model.Password);
            }
            else
            {
                _rememberedLogin.Clear();
            }

            // 前台登录面向普通用户，默认回到首页（不再跳后台）
            return RedirectToLocal(returnUrl) ?? RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "登录失败次数过多，账号已锁定，请稍后再试。");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "账号或密码错误。");
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

        var isPhone = string.Equals(model.Channel, "sms", StringComparison.OrdinalIgnoreCase);
        var target = (model.Target ?? string.Empty).Trim();
        if (!isPhone)
        {
            target = target.ToLowerInvariant();
        }

        // 手机注册账号的 UserName 即手机号，按 UserName 查重；邮箱按邮箱查重
        var user = isPhone
            ? await _userManager.FindByNameAsync(target)
            : await _userManager.FindByEmailAsync(target);

        if (model.Purpose == "reset")
        {
            // 找回密码：邮箱必须已注册（仅支持邮箱）
            if (user is null)
            {
                return Json(new { ok = false, error = _loc["EmailNotRegistered"].Value });
            }
        }
        else
        {
            // 注册：目标地址必须未注册
            if (user is not null)
            {
                return Json(new { ok = false, error = isPhone ? _loc["PhoneAlreadyRegistered"].Value : _loc["EmailAlreadyRegistered"].Value });
            }
        }

        try
        {
            var code = await _verificationCodeService.GenerateAsync(target);

            // 演示模式：验证码始终随响应返回给页面显示；邮箱渠道再尽力而为补发邮件（能发就发，失败不影响注册）
            if (!isPhone)
            {
                try
                {
                    var subject = _loc["VerificationEmailSubject"].Value;
                    var body = EmailTemplateBuilder.BuildVerificationCode(code);
                    await _emailService.SendAsync(target, subject, body);
                }
                catch
                {
                    // 忽略邮件发送失败：演示模式下验证码已在页面展示
                }
            }

            return Json(new { ok = true, code });
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
        var isPhone = string.Equals(model.RegisterBy, "phone", StringComparison.OrdinalIgnoreCase);

        // 邮箱/手机号二选一为账号标识：DataAnnotations 无法表达「二选一必填」，故在此手动校验
        if (isPhone)
        {
            if (string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                ModelState.AddModelError(nameof(model.PhoneNumber), "请输入手机号");
            }
            else if (!Regex.IsMatch(model.PhoneNumber, PhonePattern))
            {
                ModelState.AddModelError(nameof(model.PhoneNumber), "手机号格式不正确");
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "请输入邮箱");
            }
            else if (!new EmailAddressAttribute().IsValid(model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "邮箱格式不正确");
            }
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var identifier = isPhone ? model.PhoneNumber.Trim() : model.Email.Trim().ToLowerInvariant();

        // 目标地址唯一性校验（统一提示，避免泄露账号是否已存在）
        var existing = isPhone
            ? await _userManager.FindByNameAsync(identifier)
            : await _userManager.FindByEmailAsync(identifier);
        if (existing is not null)
        {
            ModelState.AddModelError(isPhone ? nameof(model.PhoneNumber) : nameof(model.Email),
                isPhone ? _loc["PhoneAlreadyRegistered"].Value : _loc["EmailAlreadyRegistered"].Value);
            return View(model);
        }

        // 校验验证码（邮箱/手机号共用同一验证码服务）
        if (!await _verificationCodeService.VerifyAsync(identifier, model.VerificationCode))
        {
            ModelState.AddModelError(nameof(model.VerificationCode), _loc["InvalidVerificationCode"].Value);
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = identifier,
            Email = isPhone ? NullIfBlank(model.Email) : identifier,
            EmailConfirmed = true, // 通过验证码校验即视为已验证
            FirstName = model.FirstName,
            LastName = model.LastName,
            Address = model.Address,
            PhoneNumber = isPhone ? identifier : NullIfBlank(model.PhoneNumber)
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

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
