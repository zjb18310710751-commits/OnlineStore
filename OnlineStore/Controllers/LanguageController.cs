using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace OnlineStore.Controllers;

public class LanguageController : Controller
{
    private static readonly string[] SupportedCultures = { "zh-CN", "en" };

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Set(string culture, string returnUrl)
    {
        if (SupportedCultures.Contains(culture))
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true
                });
        }

        var target = Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
        return LocalRedirect(target);
    }
}
