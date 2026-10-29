using System.Data.Common;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OnlineStore.Controllers;
using System.Reflection;
using OnlineStore.Infrastructure;

namespace OnlineStore.Tests.Controllers;

public class QualityPipelineTests
{
    [Fact]
    public void AllAccountCodeAndCredentialWrites_HaveTheSharedRateLimit()
    {
        foreach (var name in new[] { "Login", "Register", "SendCode", "ForgotPassword", "SendProfileEmailCode", "ChangePassword", "Profile" })
        {
            var action = typeof(AccountController).GetMethods().Single(method => method.Name == name && method.GetCustomAttribute<HttpPostAttribute>() is not null);
            Assert.Equal("account", action.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName);
        }
    }

    [Fact]
    public async Task JsonBodyWithoutAccept_ProfileCodeFailureIsStructured()
    {
        await using var app = CreateApplication();
        app.MapPost("/Account/SendProfileEmailCode", () => Task.FromException<string>(new TestDatabaseException("private")));
        await app.StartAsync();
        using var client = Client(app);
        var response = await client.PostAsync("/Account/SendProfileEmailCode", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("service_unavailable", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DeterministicDatabaseFailure_IsNotAdvertisedAsRetryable()
    {
        await using var app = CreateApplication();
        app.MapPost("/constraint", () => Task.FromException<string>(new TestDatabaseException("constraint", false)));
        await app.StartAsync();
        using var client = Client(app);
        var response = await client.PostAsync("/constraint", null);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Null(response.Headers.RetryAfter);
    }

    [Fact]
    public async Task TrustedProxy_PreservesSeparateClientBudgets()
    {
        await using var app = CreateApplication();
        app.MapPost("/account", () => Results.Ok()).RequireRateLimiting("account");
        await app.StartAsync();
        using var client = Client(app);
        for (var index = 0; index < 12; index++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/account");
            request.Headers.Add("X-Forwarded-For", $"192.0.2.{index + 1}");
            Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(request)).StatusCode);
        }
    }
    [Fact]
    public async Task DatabaseFailure_Is503WithoutSecrets_AndNextRequestRecovers()
    {
        await using var app = CreateApplication();
        var unavailable = false;
        app.MapGet("/catalog", () => unavailable
            ? Task.FromException<string>(new TestDatabaseException("PRIVATE-password-server"))
            : Task.FromResult("catalog available"));
        await app.StartAsync();
        using var client = Client(app);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/catalog")).StatusCode);
        unavailable = true;
        var failed = await client.GetAsync("/catalog");
        var body = await failed.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(5), failed.Headers.RetryAfter?.Delta);
        Assert.Contains("temporarily unavailable", body);
        Assert.DoesNotContain("PRIVATE", body);
        unavailable = false;
        Assert.Equal("catalog available", await client.GetStringAsync("/catalog"));
    }

    [Fact]
    public async Task FailedWrite_IsNeverAutomaticallyRetried_AndJsonHasNoException()
    {
        await using var app = CreateApplication();
        var attempts = 0;
        app.MapPost("/purchase", () => { attempts++; return Task.FromException<string>(new TestDatabaseException("secret")); });
        await app.StartAsync();
        using var client = Client(app);
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        var response = await client.PostAsync("/purchase", null);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(1, attempts);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("service_unavailable", body);
        Assert.DoesNotContain("secret", body);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AccountLimit_RejectsEleventhRequest_DespiteSpoofedForwardedAddresses(bool earlierTrustAll)
    {
        await using var app = CreateApplication(untrustedProxy: true, earlierTrustAll: earlierTrustAll);
        app.MapPost("/account", () => Results.Ok()).RequireRateLimiting("account");
        await app.StartAsync();
        using var client = Client(app);
        for (var index = 0; index < 11; index++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/account");
            request.Headers.Add("X-Forwarded-For", $"192.0.2.{index + 1}");
            request.Headers.Accept.ParseAdd("application/json");
            var response = await client.SendAsync(request);
            Assert.Equal(index < 10 ? HttpStatusCode.OK : HttpStatusCode.TooManyRequests, response.StatusCode);
            if (index == 10) Assert.Contains("rate_limited", await response.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task LivenessIsIndependent_ReadinessReportsDependencyFailure_HeadersPresent()
    {
        await using var app = CreateApplication();
        app.MapQualityHealthChecks();
        await app.StartAsync();
        using var client = Client(app);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/health/ready")).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/health")).StatusCode);
        var live = await client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal("nosniff", live.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Contains("frame-ancestors 'self'", live.Headers.GetValues("Content-Security-Policy").Single());
    }

    private static WebApplication CreateApplication(bool untrustedProxy = false, bool earlierTrustAll = false)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        if (earlierTrustAll)
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });
        builder.Services.AddQualityProtection(builder.Configuration);
        builder.Services.AddLocalization();
        builder.Services.AddHealthChecks().AddCheck("database", () => HealthCheckResult.Unhealthy("private"), tags: new[] { "ready" });
        var app = builder.Build();
        if (untrustedProxy)
            app.Use((context, next) => { context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.5"); return next(context); });
        app.UseForwardedHeaders();
        app.UseRequestLocalization(new RequestLocalizationOptions()
            .SetDefaultCulture("en").AddSupportedCultures("en", "zh-CN").AddSupportedUICultures("en", "zh-CN"));
        app.UseMiddleware<SecurityResponseHeadersMiddleware>();
        app.UseMiddleware<DatabaseFailureMiddleware>();
        app.UseRouting();
        app.UseRateLimiter();
        return app;
    }

    private static HttpClient Client(WebApplication app) => new()
    {
        BaseAddress = new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single())
    };

    private sealed class TestDatabaseException(string message, bool transient = true) : DbException(message)
    {
        public override bool IsTransient => transient;
    }
}
