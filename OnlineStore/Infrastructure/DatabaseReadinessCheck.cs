using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OnlineStore.Data;

namespace OnlineStore.Infrastructure;

public sealed class DatabaseReadinessCheck(ApplicationDbContext database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(3));
        try
        {
            return await database.Database.CanConnectAsync(timeout.Token)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Database is unavailable.");
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return HealthCheckResult.Unhealthy("Database readiness probe failed.");
        }
    }
}
