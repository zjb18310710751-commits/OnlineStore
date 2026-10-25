using Microsoft.AspNetCore.Http;

namespace OnlineStore.Services;

public interface IImageStorageService
{

    Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default);

    void Delete(string? relativeUrl);
}
