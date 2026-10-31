using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

namespace OnlineStore.Infrastructure;

public static class QualityProtectionExtensions
{
    public static IServiceCollection AddQualityProtection(this IServiceCollection services, IConfiguration configuration)
    {
        services.PostConfigure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            options.KnownNetworks.Clear();
            options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Loopback, 8));
            options.KnownProxies.Clear();
            options.KnownProxies.Add(IPAddress.IPv6Loopback);
            foreach (var proxy in configuration.GetSection("ReverseProxy:KnownProxies").GetChildren())
            {
                if (!IPAddress.TryParse(proxy.Value, out var address))
                    throw new InvalidOperationException("ReverseProxy:KnownProxies must contain valid IP addresses.");
                options.KnownProxies.Add(address);
            }
        });
        services.AddRateLimiter(options =>
        {
            options.AddPolicy("account", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.MapToIPv6().ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true
                }));
            options.OnRejected = async (context, cancellationToken) =>
            {
                var seconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry)
                    ? Math.Max(1, (int)Math.Ceiling(retry.TotalSeconds)) : 60;
                await FailureResponse.WriteAsync(context.HttpContext, StatusCodes.Status429TooManyRequests, "rate_limited", seconds);
            };
        });
        return services;
    }

    public static void MapQualityHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
    }
}
