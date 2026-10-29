namespace OnlineStore.Infrastructure;

public sealed class SecurityResponseHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers.XFrameOptions = "SAMEORIGIN";
            context.Response.Headers["Referrer-Policy"] = "same-origin";
            context.Response.Headers.ContentSecurityPolicy = "frame-ancestors 'self'; base-uri 'self'; object-src 'none'";
            return Task.CompletedTask;
        });
        return next(context);
    }
}
