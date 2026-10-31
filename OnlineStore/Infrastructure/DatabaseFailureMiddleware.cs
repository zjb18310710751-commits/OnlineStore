using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace OnlineStore.Infrastructure;

public sealed class DatabaseFailureMiddleware(RequestDelegate next, ILogger<DatabaseFailureMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception exception) when (!context.Response.HasStarted && IsDatabaseFailure(exception))
        {
            logger.LogError(exception, "Database request failed. Request {RequestId}; method {Method}; path {Path}",
                context.TraceIdentifier, context.Request.Method, context.Request.Path);
            context.Response.Clear();
            await FailureResponse.WriteAsync(context, StatusCodes.Status503ServiceUnavailable, "service_unavailable", 5);
        }
    }

    private static bool IsDatabaseFailure(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException sql)
                return sql.Errors.Cast<SqlError>().Any(error => error.Number is
                    -2 or 2 or 20 or 53 or 64 or 233 or 942 or 976 or 4060 or 10053 or 10054 or 10060 or 11001
                    or 40197 or 40501 or 40613 or 10928 or 10929 or 49918 or 49919 or 49920);
            if (current is DbException { IsTransient: true }) return true;
        }
        return false;
    }
}
