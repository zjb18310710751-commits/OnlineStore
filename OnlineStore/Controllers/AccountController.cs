using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using OnlineStore.Models;
using OnlineStore.Services;
using OnlineStore.Validation;
using OnlineStore.ViewModels;

namespace OnlineStore.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IVerificationCodeService _verificationCodeService;
    private readonly IRememberedLoginService _rememberedLogin;
    private readonly IImageStorageService _imageStorage;
    private readonly IDeliveryAddressService _deliveryAddressService;
    private readonly IStringLocalizer<SharedResource> _loc;

    private const string PhonePattern = @"^[0-9+\-\s()]{6,20}$";

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IVerificationCodeService verificationCodeService,
        IRememberedLoginService rememberedLogin,
        IImageStorageService imageStorage,
        IDeliveryAddressService deliveryAddressService,
        IStringLocalizer<SharedResource> localizer)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _emailService = emailService;
        _verificationCodeService = verificationCodeService;
        _rememberedLogin = rememberedLogin;
        _imageStorage = imageStorage;
        _deliveryAddressService = deliveryAddressService;
        _loc = localizer;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(BuildLoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("account")]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var identifier = model.Email.Trim();
        var user = identifier.Contains('@')
            ? await _userManager.FindByEmailAsync(identifier)
            : await _userManager.FindByNameAsync(identifier);

        if (user is null)
        {

            ModelState.AddModelError(string.Empty, _loc["LoginFailed"].Value);
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {

            if (model.RememberCredentials)
            {
                _rememberedLogin.Save(model.Email);
            }
            else
            {
                _rememberedLogin.Clear();
            }

            return RedirectToLocal(returnUrl) ?? RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, _loc["AccountLocked"].Value);
            return View(model);
        }

        ModelState.AddModelError(string.Empty, _loc["LoginFailed"].Value);
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

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("account")]
    public async Task<IActionResult> SendCode([FromBody] SendCodeViewModel model)
    {
        if (model is null || !ModelState.IsValid)
        {
            return Json(new { ok = false, error = _loc["InvalidEmail"].Value });
        }

        if (!string.Equals(model.Channel, "email", StringComparison.OrdinalIgnoreCase))
        {
            return Json(new { ok = false, error = PhoneRegistrationUnavailable() });
        }

        var target = (model.Target ?? string.Empty).Trim().ToLowerInvariant();
        if (target.Length > 256 || !new EmailAddressAttribute().IsValid(target) ||
            (model.Purpose != "reset" && model.Purpose != "register"))
        {
            return Json(new { ok = false, error = _loc["InvalidEmail"].Value });
        }

        var user = await _userManager.FindByEmailAsync(target);

        if (model.Purpose == "reset")
        {

            if (user is null)
            {
                return Json(new { ok = false, error = _loc["EmailNotRegistered"].Value });
            }
        }
        else
        {

            if (user is not null)
            {
                return Json(new { ok = false, error = _loc["EmailAlreadyRegistered"].Value });
            }
        }

        return await SendEmailVerificationCodeAsync(target);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("account")]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!string.Equals(model.RegisterBy, "email", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, PhoneRegistrationUnavailable());
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), _loc["EmailRequired"].Value);
        }
        else if (model.Email.Length > 256 || !new EmailAddressAttribute().IsValid(model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), _loc["InvalidEmail"].Value);
        }

        model.Address = AddressFormatter.Combine(
            model.AddressLine1, model.AddressLine2, model.City, model.PostalCode, model.Country);
        ModelState.Remove(nameof(model.Address));
        if (string.IsNullOrWhiteSpace(model.AddressLine1) || string.IsNullOrWhiteSpace(model.City) ||
            string.IsNullOrWhiteSpace(model.PostalCode) || string.IsNullOrWhiteSpace(model.Country))
        {
            ModelState.AddModelError(nameof(model.Address), _loc["AddressRequired"].Value);
        }
        if (model.Address.Length > 500)
        {
            ModelState.AddModelError(nameof(model.Address), _loc["CheckoutAddressTooLong"].Value);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var identifier = model.Email!.Trim().ToLowerInvariant();

        var existing = await _userManager.FindByEmailAsync(identifier);
        if (existing is not null)
        {
            ModelState.AddModelError(nameof(model.Email), _loc["EmailAlreadyRegistered"].Value);
            return View(model);
        }

        if (!await _verificationCodeService.VerifyAsync(identifier, model.VerificationCode))
        {
            ModelState.AddModelError(nameof(model.VerificationCode), _loc["InvalidVerificationCode"].Value);
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = identifier,
            Email = identifier,
            EmailConfirmed = true,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Address = model.Address,
            PhoneNumber = NullIfBlank(model.PhoneNumber)
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

        var recipientName = $"{model.LastName}{model.FirstName}".Trim();
        await _deliveryAddressService.SaveAsync(user.Id, null, new DeliveryAddressInput(
            recipientName[..Math.Min(recipientName.Length, 100)], model.PhoneNumber ?? string.Empty,
            model.AddressLine1, model.AddressLine2, model.City, model.PostalCode, model.Country, true));

        await _signInManager.SignInAsync(user, isPersistent: false);
        TempData["Success"] = _loc["RegisterSuccess"].Value;
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
    [EnableRateLimiting("account")]
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

        var vm = new ProfileViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber
        };
        PopulateProfileSidebar(vm, user, "basic");
        return View(vm);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("account")]
    public async Task<IActionResult> Profile(ProfileViewModel vm)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        ValidateBasicProfile(vm);
        if (!ModelState.IsValid)
        {
            PopulateProfileSidebar(vm, user, "basic");
            return View(vm);
        }

        user.FirstName = vm.FirstName!.Trim();
        user.LastName = vm.LastName!.Trim();
        user.PhoneNumber = vm.PhoneNumber!.Trim();
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            PopulateProfileSidebar(vm, user, "basic");
            return View(vm);
        }

        TempData["Success"] = _loc["ProfileUpdated"].Value;
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ChangePassword()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        var vm = new ChangePasswordViewModel
        {
            RegisteredEmail = GetEditableEmail(user)
        };
        PopulateProfileSidebar(vm, user, "password");
        return View(vm);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("account")]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        var normalizedEmail = vm.RegisteredEmail?.Trim().ToLowerInvariant();
        await ValidateChangePasswordAsync(vm, user, normalizedEmail);
        if (!ModelState.IsValid)
        {
            PopulateProfileSidebar(vm, user, "password");
            return View(vm);
        }

        if (!await _verificationCodeService.VerifyAsync(normalizedEmail!, vm.VerificationCode!))
        {
            ModelState.AddModelError(nameof(vm.VerificationCode), _loc["InvalidVerificationCode"]);
            PopulateProfileSidebar(vm, user, "password");
            return View(vm);
        }

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, vm.NewPassword!);
        if (!resetResult.Succeeded)
        {
            AddIdentityErrors(resetResult);
            PopulateProfileSidebar(vm, user, "password");
            return View(vm);
        }

        if (!string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            var emailResult = await _userManager.SetEmailAsync(user, normalizedEmail);
            if (!emailResult.Succeeded)
            {
                AddIdentityErrors(emailResult);
                PopulateProfileSidebar(vm, user, "password");
                return View(vm);
            }

            user.EmailConfirmed = true;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                AddIdentityErrors(updateResult);
                PopulateProfileSidebar(vm, user, "password");
                return View(vm);
            }
        }

        TempData["Success"] = _loc["PasswordUpdated"].Value;
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("account")]
    public async Task<IActionResult> SendProfileEmailCode([FromBody] SendCodeViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var target = model?.Target?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(target) || target.Length > 256 || !new EmailAddressAttribute().IsValid(target))
        {
            return Json(new { ok = false, error = _loc["InvalidEmail"].Value });
        }

        var owner = await _userManager.FindByEmailAsync(target);
        if (owner is not null && owner.Id != user.Id)
        {
            return Json(new { ok = false, error = _loc["EmailAlreadyRegistered"].Value });
        }

        return await SendEmailVerificationCodeAsync(target);
    }

    private async Task<IActionResult> SendEmailVerificationCodeAsync(string target)
    {
        string code;
        try
        {
            code = await _verificationCodeService.GenerateAsync(target);
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { ok = false, error = ex.Message });
        }

        try
        {
            await _emailService.SendAsync(target, EmailTemplateBuilder.VerificationCodeSubject,
                EmailTemplateBuilder.BuildVerificationCode(code));
        }
        catch (Exception)
        {
            return Json(new { ok = false, error = _loc["SendCodeFailed"].Value });
        }

        return Json(new { ok = true });
    }

    private static string PhoneRegistrationUnavailable() => L10n.Text(
        "短信验证暂不可用，请使用邮箱注册。", "SMS verification is unavailable. Please register with email.");

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ChangeAddress([FromQuery] bool embedded = false)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        await _deliveryAddressService.EnsureLegacyAddressAsync(user);
        var vm = new AddressViewModel();
        await PopulateAddressBookAsync(vm, user);
        ViewData["Embedded"] = embedded;
        return View(vm);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeAddress(
        AddressViewModel vm,
        [FromForm] bool embedded = false)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        ViewData["Embedded"] = embedded;

        if (string.IsNullOrWhiteSpace(vm.RecipientName))
        {
            ModelState.AddModelError(nameof(vm.RecipientName), _loc["RecipientNameRequired"]);
        }

        if (string.IsNullOrWhiteSpace(vm.PhoneNumber))
        {
            ModelState.AddModelError(nameof(vm.PhoneNumber), _loc["PhoneNumberRequired"]);
        }
        else if (!Regex.IsMatch(vm.PhoneNumber.Trim(), PhonePattern))
        {
            ModelState.AddModelError(nameof(vm.PhoneNumber), _loc["InvalidPhone"]);
        }

        if (string.IsNullOrWhiteSpace(vm.Address))
        {
            ModelState.AddModelError(nameof(vm.Address), _loc["CheckoutAddressRequired"]);
        }

        if (string.IsNullOrWhiteSpace(vm.City))
        {
            ModelState.AddModelError(nameof(vm.City), _loc["CityRequired"]);
        }

        if (string.IsNullOrWhiteSpace(vm.PostalCode))
        {
            ModelState.AddModelError(nameof(vm.PostalCode), _loc["PostalCodeRequired"]);
        }

        if (string.IsNullOrWhiteSpace(vm.Country))
        {
            ModelState.AddModelError(nameof(vm.Country), _loc["CountryRequired"]);
        }

        var fullAddress = AddressFormatter.Combine(
            vm.Address,
            vm.AddressLine2,
            vm.City,
            vm.PostalCode,
            vm.Country);

        if (fullAddress.Length > 500)
        {
            ModelState.AddModelError(nameof(vm.Address), _loc["CheckoutAddressTooLong"]);
        }

        if (!ModelState.IsValid)
        {
            await PopulateAddressBookAsync(vm, user);
            return View(vm);
        }

        var saved = await _deliveryAddressService.SaveAsync(
            user.Id,
            vm.Id,
            new DeliveryAddressInput(
                vm.RecipientName,
                vm.PhoneNumber,
                vm.Address,
                vm.AddressLine2,
                vm.City,
                vm.PostalCode,
                vm.Country,
                vm.IsDefault));
        if (saved is null)
        {
            ModelState.AddModelError(string.Empty, _loc["AddressSaveFailed"]);
            await PopulateAddressBookAsync(vm, user);
            return View(vm);
        }

        TempData["Success"] = (vm.Id.HasValue ? _loc["AddressUpdated"] : _loc["AddressAdded"]).Value;
        return RedirectToAction(nameof(ChangeAddress), new { embedded });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAddress(
        int id,
        [FromForm] bool embedded = false)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!await _deliveryAddressService.DeleteAsync(user.Id, id))
        {
            TempData["Error"] = _loc["AddressDeleteFailed"].Value;
            return RedirectToAction(nameof(ChangeAddress), new { embedded });
        }

        TempData["Success"] = _loc["AddressDeleted"].Value;
        return RedirectToAction(nameof(ChangeAddress), new { embedded });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefaultAddress(
        int id,
        [FromForm] bool embedded = false)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!await _deliveryAddressService.SetDefaultAsync(user.Id, id))
        {
            TempData["Error"] = _loc["AddressSetDefaultFailed"].Value;
            return RedirectToAction(nameof(ChangeAddress), new { embedded });
        }

        TempData["Success"] = _loc["AddressDefaultUpdated"].Value;
        return RedirectToAction(nameof(ChangeAddress), new { embedded });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAvatar(IFormFile? avatarFile)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (avatarFile is null || avatarFile.Length == 0)
        {
            TempData["Error"] = _loc["AvatarFileRequired"].Value;
            return RedirectToAction(nameof(Profile));
        }

        string newAvatarUrl;
        try
        {
            newAvatarUrl = await _imageStorage.SaveAsync(avatarFile);
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Profile));
        }

        var previousAvatarUrl = user.AvatarUrl;
        user.AvatarUrl = newAvatarUrl;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            _imageStorage.Delete(newAvatarUrl);
            AddIdentityErrors(result);
            TempData["Error"] = string.Join(" ", ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage));
            return RedirectToAction(nameof(Profile));
        }

        _imageStorage.Delete(previousAvatarUrl);
        TempData["Success"] = _loc["AvatarUpdated"].Value;
        return RedirectToAction(nameof(Profile));
    }

    private void ValidateBasicProfile(ProfileViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(vm.FirstName))
        {
            ModelState.AddModelError(nameof(vm.FirstName), _loc["FirstNameRequired"]);
        }
        else if (vm.FirstName.Length > 100)
        {
            ModelState.AddModelError(nameof(vm.FirstName), _loc["FirstNameTooLong"]);
        }

        if (string.IsNullOrWhiteSpace(vm.LastName))
        {
            ModelState.AddModelError(nameof(vm.LastName), _loc["LastNameRequired"]);
        }
        else if (vm.LastName.Length > 100)
        {
            ModelState.AddModelError(nameof(vm.LastName), _loc["LastNameTooLong"]);
        }

        if (string.IsNullOrWhiteSpace(vm.PhoneNumber))
        {
            ModelState.AddModelError(nameof(vm.PhoneNumber), _loc["PhoneNumberRequired"]);
        }
        else if (!Regex.IsMatch(vm.PhoneNumber, PhonePattern))
        {
            ModelState.AddModelError(nameof(vm.PhoneNumber), _loc["InvalidPhone"]);
        }
    }

    private async Task ValidateChangePasswordAsync(
        ChangePasswordViewModel vm,
        ApplicationUser user,
        string? normalizedEmail)
    {
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            ModelState.AddModelError(nameof(vm.RegisteredEmail), _loc["EmailRequired"]);
        }
        else if (!new EmailAddressAttribute().IsValid(normalizedEmail))
        {
            ModelState.AddModelError(nameof(vm.RegisteredEmail), _loc["InvalidEmail"]);
        }
        else
        {
            var owner = await _userManager.FindByEmailAsync(normalizedEmail);
            if (owner is not null && owner.Id != user.Id)
            {
                ModelState.AddModelError(nameof(vm.RegisteredEmail), _loc["EmailAlreadyRegistered"]);
            }
        }

        if (string.IsNullOrWhiteSpace(vm.NewPassword))
        {
            ModelState.AddModelError(nameof(vm.NewPassword), _loc["NewPasswordRequired"]);
        }
        else if (!Regex.IsMatch(vm.NewPassword, PasswordPolicy.Pattern))
        {
            ModelState.AddModelError(nameof(vm.NewPassword), _loc["PasswordPolicyError"]);
        }

        if (string.IsNullOrWhiteSpace(vm.ConfirmPassword))
        {
            ModelState.AddModelError(nameof(vm.ConfirmPassword), _loc["ConfirmPasswordRequired"]);
        }
        else if (!string.Equals(vm.NewPassword, vm.ConfirmPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(vm.ConfirmPassword), _loc["PasswordsDoNotMatch"]);
        }

        if (string.IsNullOrWhiteSpace(vm.VerificationCode))
        {
            ModelState.AddModelError(nameof(vm.VerificationCode), _loc["VerificationCodeRequired"]);
        }
        else if (!Regex.IsMatch(vm.VerificationCode, @"^\d{6}$"))
        {
            ModelState.AddModelError(nameof(vm.VerificationCode), _loc["VerificationCodeFormat"]);
        }
    }

    private void PopulateProfileSidebar(
        ProfileSidebarViewModel vm,
        ApplicationUser user,
        string activeSection)
    {
        vm.UserName = user.UserName;
        vm.Email = user.Email;
        vm.AvatarUrl = user.AvatarUrl;
        vm.DisplayName = string.IsNullOrWhiteSpace($"{user.LastName}{user.FirstName}")
            ? user.UserName
            : $"{user.LastName}{user.FirstName}";
        vm.ActiveSection = activeSection;
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    private async Task PopulateAddressBookAsync(AddressViewModel vm, ApplicationUser user)
    {
        var addresses = await _deliveryAddressService.GetAsync(user.Id);
        vm.Addresses = addresses.Select(address => new DeliveryAddressItemViewModel
        {
            Id = address.Id,
            RecipientName = address.RecipientName,
            PhoneNumber = address.PhoneNumber,
            AddressLine1 = address.AddressLine1,
            AddressLine2 = address.AddressLine2,
            City = address.City,
            PostalCode = address.PostalCode,
            Country = address.Country,
            IsDefault = address.IsDefault
        }).ToList();
        PopulateProfileSidebar(vm, user, "address");
    }

    private static string? GetEditableEmail(ApplicationUser user) =>
        user.Email ?? (user.UserName?.Contains('@') == true ? user.UserName : null);

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
