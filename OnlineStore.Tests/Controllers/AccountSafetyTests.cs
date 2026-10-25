using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OnlineStore.Controllers;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.Services;
using OnlineStore.ViewModels;

namespace OnlineStore.Tests.Controllers;

public class AccountSafetyTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EmailCode_IsDeliveredWithoutReturningItToCaller(bool profile)
    {
        using var fixture = new Fixture();
        var user = await fixture.AddUser();
        fixture.SignIn(user);
        var request = new SendCodeViewModel { Target = user.Email!, Purpose = "reset" };

        var response = profile
            ? await fixture.Controller.SendProfileEmailCode(request)
            : await fixture.Controller.SendCode(request);

        var json = JsonSerializer.SerializeToElement(Assert.IsType<JsonResult>(response).Value);
        Assert.True(json.GetProperty("ok").GetBoolean());
        Assert.False(json.TryGetProperty("code", out _));
        var code = await fixture.Context.EmailVerificationCodes.SingleAsync();
        Assert.Contains(code.Code, fixture.Email.Body);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EmailCode_DeliveryFailureIsNotReportedAsSuccess(bool profile)
    {
        using var fixture = new Fixture();
        var user = await fixture.AddUser();
        fixture.SignIn(user);
        fixture.Email.Fail = true;
        var request = new SendCodeViewModel { Target = user.Email!, Purpose = "reset" };

        var response = profile
            ? await fixture.Controller.SendProfileEmailCode(request)
            : await fixture.Controller.SendCode(request);

        var json = JsonSerializer.SerializeToElement(Assert.IsType<JsonResult>(response).Value);
        Assert.False(json.GetProperty("ok").GetBoolean());
        Assert.False(json.TryGetProperty("code", out _));
        Assert.DoesNotContain("provider failure details", json.ToString());
    }

    [Fact]
    public async Task SmsCode_WithoutDeliveryProvider_DoesNotIssueUsableCode()
    {
        using var fixture = new Fixture();
        var response = await fixture.Controller.SendCode(new SendCodeViewModel
        {
            Target = "13800138000", Channel = "sms"
        });

        var json = JsonSerializer.SerializeToElement(Assert.IsType<JsonResult>(response).Value);
        Assert.False(json.GetProperty("ok").GetBoolean());
        Assert.Empty(await fixture.Context.EmailVerificationCodes.ToListAsync());
    }

    [Fact]
    public async Task PhoneRegistration_CannotClaimExistingEmail()
    {
        using var fixture = new Fixture();
        var existing = await fixture.AddUser();
        var form = Registration(existing.Email!);
        form.RegisterBy = "phone";
        form.PhoneNumber = "13800138000";
        form.VerificationCode = await fixture.Codes.GenerateAsync(form.PhoneNumber);

        Assert.IsType<ViewResult>(await fixture.Controller.Register(form));
        Assert.Single(await fixture.Context.Users.ToListAsync());
    }

    [Fact]
    public async Task Registration_PreservesStructuredFieldsWhenStreetContainsCommaAndLineTwoIsEmpty()
    {
        using var fixture = new Fixture();
        var form = Registration("new@example.com");
        form.VerificationCode = await fixture.Codes.GenerateAsync(form.Email!);

        Assert.IsType<RedirectToActionResult>(await fixture.Controller.Register(form));

        var address = await fixture.Context.DeliveryAddresses.SingleAsync();
        Assert.Equal("12 Road, East", address.AddressLine1);
        Assert.Null(address.AddressLine2);
        Assert.Equal("London", address.City);
        Assert.Equal("SW1A 1AA", address.PostalCode);
        Assert.Equal("UK", address.Country);
    }

    [Theory]
    [InlineData(nameof(AddressViewModel.RecipientName), 101)]
    [InlineData(nameof(AddressViewModel.Address), 201)]
    [InlineData(nameof(AddressViewModel.AddressLine2), 201)]
    [InlineData(nameof(AddressViewModel.City), 101)]
    [InlineData(nameof(AddressViewModel.PostalCode), 101)]
    [InlineData(nameof(AddressViewModel.Country), 101)]
    public void AddressForm_RejectsValuesThatExceedDatabaseColumns(string field, int length)
    {
        var form = new AddressViewModel
        {
            RecipientName = "A Customer", PhoneNumber = "123456789", Address = "12 Road",
            City = "London", PostalCode = "SW1A 1AA", Country = "UK"
        };
        typeof(AddressViewModel).GetProperty(field)!.SetValue(form, new string('x', length));
        var errors = new List<ValidationResult>();

        Assert.False(Validator.TryValidateObject(form, new ValidationContext(form), errors, true));
        Assert.Contains(errors, error => error.MemberNames.Contains(field));
    }

    [Fact]
    public async Task LegacyAddress_WithoutSecondLine_PreservesCityPostcodeAndCountry()
    {
        using var fixture = new Fixture();
        var user = await fixture.AddUser();
        user.Address = "12 Road, London, SW1A 1AA, UK";
        await fixture.Context.SaveChangesAsync();

        await new DeliveryAddressService(fixture.Context).EnsureLegacyAddressAsync(user);

        var address = await fixture.Context.DeliveryAddresses.SingleAsync();
        Assert.Null(address.AddressLine2);
        Assert.Equal("London", address.City);
        Assert.Equal("SW1A 1AA", address.PostalCode);
        Assert.Equal("UK", address.Country);
    }

    [Fact]
    public async Task Review_RejectsOversizedCommentBeforeWriting()
    {
        using var fixture = new Fixture();
        var user = await fixture.AddUser();
        fixture.SignIn(user);
        fixture.Context.Products.Add(new Product { Id = 1, Name = "Product" });
        await fixture.Context.SaveChangesAsync();
        var controller = fixture.Reviews();

        await controller.Create(1, 5, new string('x', 1001), null);

        Assert.Empty(await fixture.Context.Reviews.ToListAsync());
        Assert.True(controller.TempData.ContainsKey("Error"));
    }

    [Fact]
    public async Task Review_BoundsAccountDisplayNameToStorageLimit()
    {
        using var fixture = new Fixture();
        var user = await fixture.AddUser();
        user.FirstName = new string('a', 60);
        user.LastName = new string('b', 60);
        await fixture.Users.UpdateAsync(user);
        fixture.SignIn(user);
        fixture.Context.Products.Add(new Product { Id = 1, Name = "Product" });
        await fixture.Context.SaveChangesAsync();

        await fixture.Reviews().Create(1, 5, "Good", null);

        var review = await fixture.Context.Reviews.SingleAsync();
        Assert.InRange(review.UserName.Length, 1, 50);
        Assert.Equal("Good", review.Comment);
    }

    private static RegisterViewModel Registration(string email) => new()
    {
        Email = email, FirstName = "A", LastName = "Customer", PhoneNumber = "123456789",
        Address = "12 Road, East, London, SW1A 1AA, UK", AddressLine1 = "12 Road, East",
        City = "London", PostalCode = "SW1A 1AA", Country = "UK",
        Password = "Secure!123", ConfirmPassword = "Secure!123"
    };

    private sealed class Fixture : IDisposable
    {
        private readonly ServiceProvider _services;
        private readonly DefaultHttpContext _http;
        private readonly TestLocalizer _loc = new();
        public ApplicationDbContext Context { get; }
        public UserManager<ApplicationUser> Users { get; }
        public VerificationCodeService Codes { get; }
        public TestEmail Email { get; } = new();
        public AccountController Controller { get; }

        public Fixture()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
            _services = services.BuildServiceProvider();
            Context = _services.GetRequiredService<ApplicationDbContext>();
            Users = _services.GetRequiredService<UserManager<ApplicationUser>>();
            _http = new DefaultHttpContext { RequestServices = _services };
            _services.GetRequiredService<IHttpContextAccessor>().HttpContext = _http;
            Codes = new VerificationCodeService(Context);
            Controller = new AccountController(_services.GetRequiredService<SignInManager<ApplicationUser>>(),
                Users, Email, Codes, null!, null!, new DeliveryAddressService(Context), _loc)
            {
                ControllerContext = new ControllerContext { HttpContext = _http },
                Url = new UrlHelper(new ActionContext(_http, new RouteData(), new ActionDescriptor())),
                TempData = new TempDataDictionary(_http, new EmptyTempDataProvider())
            };
        }

        public async Task<ApplicationUser> AddUser()
        {
            var user = new ApplicationUser { UserName = "user@example.com", Email = "user@example.com" };
            Assert.True((await Users.CreateAsync(user, "Secure!123")).Succeeded);
            return user;
        }

        public void SignIn(ApplicationUser user) => _http.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, user.Id) }, "test"));

        public ReviewsController Reviews() => new(new ReviewService(Context), new ProductService(Context), null!, Users, _loc)
        {
            ControllerContext = new ControllerContext { HttpContext = _http },
            Url = new UrlHelper(new ActionContext(_http, new RouteData(), new ActionDescriptor())),
            TempData = new TempDataDictionary(_http, new EmptyTempDataProvider())
        };

        public void Dispose() => _services.Dispose();
    }

    private sealed class TestEmail : IEmailService
    {
        public string Body { get; private set; } = string.Empty;
        public bool Fail { get; set; }
        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            if (Fail) throw new InvalidOperationException("provider failure details");
            Body = htmlBody;
            return Task.CompletedTask;
        }
    }

    private sealed class TestLocalizer : IStringLocalizer<SharedResource>
    {
        public LocalizedString this[string name] => new(name, name);
        public LocalizedString this[string name, params object[] arguments] => new(name, name);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
    }

    private sealed class EmptyTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
