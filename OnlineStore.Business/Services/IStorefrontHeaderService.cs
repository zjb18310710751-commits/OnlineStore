using System.Security.Claims;
using OnlineStore.ViewModels;

namespace OnlineStore.Services;

public interface IStorefrontHeaderService
{
    Task<StorefrontHeaderViewModel> GetAsync(ClaimsPrincipal principal);
}
