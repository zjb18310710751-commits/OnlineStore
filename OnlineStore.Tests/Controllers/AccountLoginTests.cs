using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OnlineStore.Controllers;
using OnlineStore.Services;
using OnlineStore.ViewModels;

namespace OnlineStore.Tests.Controllers;

public class AccountLoginTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("/Cart")]
    [InlineData("/Admin")]
    [InlineData("/Admin/Products")]
    public void Login_UsesSharedLoginView_AndPreservesReturnUrl(string? returnUrl)
    {
        var controller = new AccountController(
            null!, null!, null!, null!, new EmptyRememberedLoginService(), null!, null!, null!)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };

        Assert.False(controller.User.Identity!.IsAuthenticated);

        var result = controller.Login(returnUrl);

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<LoginViewModel>(view.Model);
        Assert.Equal(returnUrl, view.ViewData["ReturnUrl"] as string);
    }

    [Fact]
    public void AdminControllers_ContinueToRequireAdminRole_WithoutAnonymousActions()
    {
        var controllers = typeof(AccountController).Assembly.GetTypes()
            .Where(type => type.Namespace == "OnlineStore.Areas.Admin.Controllers" &&
                !type.IsAbstract && typeof(Controller).IsAssignableFrom(type))
            .ToArray();

        Assert.NotEmpty(controllers);
        foreach (var controller in controllers)
        {
            Assert.Contains(controller.GetCustomAttributes<AuthorizeAttribute>(inherit: true),
                authorization => authorization.Roles == "Admin");
            Assert.Empty(controller.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true));

            foreach (var action in controller.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                Assert.Empty(action.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true));
            }
        }
    }

    private sealed class EmptyRememberedLoginService : IRememberedLoginService
    {
        public RememberedLogin? Load() => null;

        public void Save(string email) =>
            throw new InvalidOperationException("Displaying the login form must not save credentials.");

        public void Clear() =>
            throw new InvalidOperationException("Displaying the login form must not clear credentials.");
    }
}
