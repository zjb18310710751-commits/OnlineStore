using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using OnlineStore;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("未配置连接字符串 ConnectionStrings:DefaultConnection。");

var databaseRetryCount = builder.Configuration.GetValue("Database:RetryCount", 3);
var databaseRetryDelaySeconds = builder.Configuration.GetValue("Database:MaxRetryDelaySeconds", 3);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(
        maxRetryCount: Math.Max(0, databaseRetryCount),
        maxRetryDelay: TimeSpan.FromSeconds(Math.Max(0, databaseRetryDelaySeconds)),
        errorNumbersToAdd: null)));

builder.Services.Configure<InitializationOptions>(
    builder.Configuration.GetSection(InitializationOptions.SectionName));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IStorefrontHeaderService, StorefrontHeaderService>();
builder.Services.AddScoped<IHeroSlideService, HeroSlideService>();
builder.Services.AddScoped<IShopService, ShopService>();
builder.Services.AddScoped<IProductSkuService, ProductSkuService>();
builder.Services.AddScoped<IProductSpecService, ProductSpecService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddHostedService<OrderConfirmationBackgroundService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IDeliveryAddressService, DeliveryAddressService>();
builder.Services.AddScoped<ICartStore, SessionCartStore>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IImageStorageService, ImageStorageService>();
builder.Services.AddScoped<BrevoApiEmailService>();
builder.Services.AddScoped<SmtpEmailService>();
builder.Services.AddScoped<IEmailService>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var provider = configuration["Email:Provider"]?.Trim();
    var useSmtp = string.Equals(provider, "Smtp", StringComparison.OrdinalIgnoreCase)
        || (string.IsNullOrWhiteSpace(provider)
            && !string.IsNullOrWhiteSpace(configuration["Email:Host"]));

    return useSmtp
        ? serviceProvider.GetRequiredService<SmtpEmailService>()
        : serviceProvider.GetRequiredService<BrevoApiEmailService>();
});
builder.Services.AddScoped<IVerificationCodeService, VerificationCodeService>();
builder.Services.AddScoped<ICouponService, CouponService>();
builder.Services.AddScoped<IRememberedLoginService, RememberedLoginCookieService>();

var sessionProvider = builder.Configuration["Session:Provider"]?.Trim();
var useSqlServerSession = string.Equals(sessionProvider, "SqlServer", StringComparison.OrdinalIgnoreCase)
    || (string.IsNullOrWhiteSpace(sessionProvider) && builder.Environment.IsProduction());
if (useSqlServerSession)
{
    builder.Services.AddDistributedSqlServerCache(options =>
    {
        options.ConnectionString = connectionString;
        options.SchemaName = builder.Configuration["Session:SchemaName"] ?? "dbo";
        options.TableName = builder.Configuration["Session:TableName"] ?? "SessionCache";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

var authenticationPermitLimit = builder.Configuration.GetValue(
    "Security:AuthenticationRateLimit:PermitLimit",
    10);
var authenticationWindowSeconds = builder.Configuration.GetValue(
    "Security:AuthenticationRateLimit:WindowSeconds",
    60);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("authentication", context =>
    {
        var clientKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            clientKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = Math.Max(1, authenticationPermitLimit),
                Window = TimeSpan.FromSeconds(Math.Max(1, authenticationWindowSeconds)),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database");

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddHttpClient();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IOrderNumberGenerator, OrderNumberGenerator>();

builder.Services.AddControllersWithViews()

    .AddDataAnnotationsLocalization(options =>
        options.DataAnnotationLocalizerProvider = (type, factory) => factory.Create(typeof(SharedResource)));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseForwardedHeaders();
    app.UseHttpsRedirection();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStatusCodePagesWithReExecute("/Home/Error", "?code={0}");
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    headers["Cross-Origin-Opener-Policy"] = "same-origin";
    await next();
});

var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
var uploadsRoot = UploadStoragePath.Prepare(app.Environment, app.Configuration, startupLogger);
app.UseStaticFiles(new StaticFileOptions
{
    RequestPath = UploadStoragePath.RequestPath,
    FileProvider = new PhysicalFileProvider(uploadsRoot),
    OnPrepareResponse = context =>
    {
        context.Context.Response.Headers.CacheControl = "public,max-age=604800,immutable";
    }
});
app.UseStaticFiles();

var supportedCultures = new[] { "zh-CN", "en" };
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("zh-CN")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures));

app.UseRouting();
app.UseRateLimiter();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHealthChecks("/health");

if (app.Configuration.GetValue("Initialization:RunOnStartup", true))
{
    try
    {
        await DbInitializer.InitializeAsync(app.Services);
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
        logger.LogError(ex, "数据库初始化失败：请检查 SQL Server (SQLEXPRESS) 服务是否已启动，以及连接串/凭据是否正确。");
        var failFast = app.Configuration.GetValue<bool?>("Initialization:FailFastOnInitializationFailure")
            ?? app.Environment.IsProduction();
        if (failFast)
        {
            throw;
        }
    }
}

app.Run();
