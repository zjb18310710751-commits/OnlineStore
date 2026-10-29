using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using OnlineStore;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.Services;
using OnlineStore.Infrastructure;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);
var keyDirectory = builder.Configuration["DataProtection:KeyDirectory"];
if (!string.IsNullOrWhiteSpace(keyDirectory))
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keyDirectory));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("未配置连接字符串 ConnectionStrings:DefaultConnection。");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
        sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.GetName().Name)));

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
builder.Services.AddScoped<IHeroSlideService, HeroSlideService>();
builder.Services.AddScoped<IShopService, ShopService>();
builder.Services.AddScoped<IProductSkuService, ProductSkuService>();
builder.Services.AddScoped<IProductSpecService, ProductSpecService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddSingleton<IOrderShippingPolicy, OrderShippingPolicy>();
builder.Services.AddHostedService<OrderConfirmationBackgroundService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IDeliveryAddressService, DeliveryAddressService>();
builder.Services.AddScoped<ICartStore, SessionCartStore>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IImageStorageService, ImageStorageService>();
builder.Services.AddScoped<IEmailService>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var provider = configuration["Email:Provider"];

    return string.Equals(provider, "Brevo", StringComparison.OrdinalIgnoreCase)
        ? ActivatorUtilities.CreateInstance<BrevoApiEmailService>(serviceProvider)
        : ActivatorUtilities.CreateInstance<SmtpEmailService>(serviceProvider);
});
builder.Services.AddScoped<IVerificationCodeService, VerificationCodeService>();
builder.Services.AddScoped<ICouponService, CouponService>();
builder.Services.AddScoped<IRememberedLoginService, RememberedLoginCookieService>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

builder.Services.AddQualityProtection(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseReadinessCheck>("database", tags: new[] { "ready" }, timeout: TimeSpan.FromSeconds(3));

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddHttpClient();

builder.Services.AddControllersWithViews()

    .AddDataAnnotationsLocalization(options =>
        options.DataAnnotationLocalizerProvider = (type, factory) => factory.Create(typeof(SharedResource)));

var app = builder.Build();
app.UseMiddleware<SecurityResponseHeadersMiddleware>();
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStatusCodePagesWithReExecute("/Home/Error", "?code={0}");

app.UseStaticFiles();

var supportedCultures = new[] { "zh-CN", "en" };
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("zh-CN")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures));

app.UseRouting();
app.UseMiddleware<DatabaseFailureMiddleware>();
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

app.MapQualityHealthChecks();

try
{
    await DbInitializer.InitializeAsync(app.Services);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
    logger.LogError(ex, "数据库初始化失败：请检查 SQL Server (SQLEXPRESS) 服务是否已启动，以及连接串/凭据是否正确。");
}

app.Run();
