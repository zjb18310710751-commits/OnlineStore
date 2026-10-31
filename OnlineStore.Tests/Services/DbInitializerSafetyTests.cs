using System.Reflection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using OnlineStore.Data;
using OnlineStore.Models;

namespace OnlineStore.Tests.Services;

public class DbInitializerSafetyTests
{
    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task NonDevelopment_WithoutBootstrapDoesNotCreateAccountsOrDemoOrders(string environment)
    {
        await using var fixture = await Fixture.CreateAsync(environment);

        await fixture.SeedAsync();

        Assert.Empty(await fixture.Context.Users.ToListAsync());
        Assert.Empty(await fixture.Context.Orders.ToListAsync());
    }

    [Fact]
    public async Task Production_ExplicitBootstrapCreatesOnlyConfiguredAdministrator()
    {
        await using var fixture = await Fixture.CreateAsync(Environments.Production,
            ("Bootstrap:AdminEmail", "owner@store.example"), ("Bootstrap:AdminPassword", "Owner@Keeps123"));

        await fixture.SeedAsync();

        var admin = Assert.Single(await fixture.Context.Users.ToListAsync());
        Assert.Equal("owner@store.example", admin.Email);
        Assert.True(await fixture.Users.CheckPasswordAsync(admin, "Owner@Keeps123"));
        Assert.True(await fixture.Users.IsInRoleAsync(admin, DbInitializer.AdminRole));
        Assert.Empty(await fixture.Context.Orders.ToListAsync());
    }

    [Fact]
    public async Task Production_PartialBootstrapDoesNotFallBackToPublicCredentials()
    {
        await using var fixture = await Fixture.CreateAsync(Environments.Production,
            ("Bootstrap:AdminEmail", "owner@store.example"));

        await fixture.SeedAsync();

        Assert.Empty(await fixture.Context.Users.ToListAsync());
    }

    [Fact]
    public async Task Production_InvalidBootstrapPasswordIsNotIncludedInErrors()
    {
        const string invalidPassword = "sensitive-low-entropy";
        await using var fixture = await Fixture.CreateAsync(Environments.Production,
            ("Bootstrap:AdminEmail", "owner@store.example"), ("Bootstrap:AdminPassword", invalidPassword));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(fixture.SeedAsync);

        Assert.DoesNotContain(invalidPassword, exception.ToString());
        Assert.Empty(await fixture.Context.Users.ToListAsync());
    }

    [Fact]
    public async Task Production_ExistingAdministratorRemainsUnchangedWithoutBootstrapSettings()
    {
        await using var fixture = await Fixture.CreateAsync(Environments.Production);
        var existing = await fixture.CreateUserAsync("existing-admin@store.example", "Owner@Keeps123");
        var roles = fixture.Context.Roles;
        roles.Add(new IdentityRole(DbInitializer.AdminRole) { NormalizedName = DbInitializer.AdminRole.ToUpperInvariant() });
        await fixture.Context.SaveChangesAsync();
        Assert.True((await fixture.Users.AddToRoleAsync(existing, DbInitializer.AdminRole)).Succeeded);
        var passwordHash = existing.PasswordHash;

        await fixture.SeedAsync();

        Assert.Single(await fixture.Context.Users.ToListAsync());
        Assert.Equal(passwordHash, existing.PasswordHash);
        Assert.True(await fixture.Users.IsInRoleAsync(existing, DbInitializer.AdminRole));
    }

    [Fact]
    public async Task Bootstrap_DoesNotPromoteExistingCustomerOrReplacePassword()
    {
        await using var fixture = await Fixture.CreateAsync(Environments.Production,
            ("Bootstrap:AdminEmail", "owner@store.example"), ("Bootstrap:AdminPassword", "Bootstrap@12345"));
        var existing = await fixture.CreateUserAsync("owner@store.example", "Owner@Keeps123");
        var passwordHash = existing.PasswordHash;

        await fixture.SeedAsync();

        Assert.Single(await fixture.Context.Users.ToListAsync());
        Assert.False(await fixture.Users.IsInRoleAsync(existing, DbInitializer.AdminRole));
        Assert.Equal(passwordHash, existing.PasswordHash);
        Assert.True(await fixture.Users.CheckPasswordAsync(existing, "Owner@Keeps123"));
    }

    [Fact]
    public async Task Development_PreservesExistingLegacyBuyerPasswordAndOrders()
    {
        await using var fixture = await Fixture.CreateAsync(Environments.Development);
        var existing = await fixture.CreateUserAsync("3293906920@qq.com", "Owner@Keeps123");
        var passwordHash = existing.PasswordHash;
        fixture.Context.Orders.Add(new Order
        {
            OrderNo = "DEMO-EXISTING", UserId = existing.Id, UserName = "Existing buyer",
            Status = OrderStatus.PendingShipment, Total = 42m
        });
        await fixture.Context.SaveChangesAsync();

        await fixture.SeedAsync();

        Assert.Equal(passwordHash, existing.PasswordHash);
        Assert.True(await fixture.Users.CheckPasswordAsync(existing, "Owner@Keeps123"));
        var order = Assert.Single(await fixture.Context.Orders.ToListAsync());
        Assert.Equal("DEMO-EXISTING", order.OrderNo);
        Assert.Equal(existing.Id, order.UserId);
        Assert.Equal(42m, order.Total);
    }

    [Fact]
    public async Task Development_CreatesDemoAccountsAndDoesNotResetThemOnLaterStartup()
    {
        await using var fixture = await Fixture.CreateAsync(Environments.Development);
        await fixture.SeedAsync();
        var admin = await fixture.Users.FindByEmailAsync("admin@store.com");
        var buyer = await fixture.Users.FindByEmailAsync("buyer@store.example");
        Assert.NotNull(admin);
        Assert.NotNull(buyer);
        Assert.True(await fixture.Users.IsInRoleAsync(admin, DbInitializer.AdminRole));
        Assert.True(await fixture.Users.CheckPasswordAsync(admin, "Admin@123456"));
        Assert.True(await fixture.Users.CheckPasswordAsync(buyer, "Admin@123456"));
        Assert.Equal(5, await fixture.Context.Orders.CountAsync());
        Assert.True((await fixture.Users.ChangePasswordAsync(buyer, "Admin@123456", "Buyer@Keeps123")).Succeeded);
        var buyerPasswordHash = buyer.PasswordHash;

        await fixture.SeedAsync();

        Assert.Equal(buyerPasswordHash, buyer.PasswordHash);
        Assert.True(await fixture.Users.CheckPasswordAsync(buyer, "Buyer@Keeps123"));
        Assert.Equal(5, await fixture.Context.Orders.CountAsync());
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly AsyncServiceScope _scope;
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _environment;
        public ApplicationDbContext Context { get; }
        public UserManager<ApplicationUser> Users { get; }
        private RoleManager<IdentityRole> Roles { get; }

        private Fixture(ServiceProvider provider, IConfiguration configuration, IHostEnvironment environment)
        {
            _provider = provider;
            _scope = provider.CreateAsyncScope();
            Context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Users = _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Roles = _scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            _configuration = configuration;
            _environment = environment;
        }

        public static async Task<Fixture> CreateAsync(string environment, params (string Key, string Value)[] settings)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(
                settings.ToDictionary(item => item.Key, item => (string?)item.Value)).Build();
            var fixture = new Fixture(services.BuildServiceProvider(), configuration,
                new HostEnvironment { EnvironmentName = environment });
            fixture.Context.Categories.Add(new Category { Name = "Category" });
            fixture.Context.Products.Add(new Product { Name = "Product", Price = 20m, Stock = 10, ShopId = 1 });
            await fixture.Context.SaveChangesAsync();
            return fixture;
        }

        public async Task<ApplicationUser> CreateUserAsync(string email, string password)
        {
            var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            Assert.True((await Users.CreateAsync(user, password)).Succeeded);
            return user;
        }

        public Task SeedAsync()
        {
            var method = typeof(DbInitializer).GetMethod("SeedAsync", BindingFlags.Static | BindingFlags.NonPublic)!;
            var values = new Dictionary<Type, object>
            {
                [typeof(ApplicationDbContext)] = Context,
                [typeof(UserManager<ApplicationUser>)] = Users,
                [typeof(RoleManager<IdentityRole>)] = Roles,
                [typeof(InitializationOptions)] = new InitializationOptions(),
                [typeof(IConfiguration)] = _configuration,
                [typeof(IHostEnvironment)] = _environment
            };
            return (Task)method.Invoke(null, method.GetParameters().Select(parameter => values[parameter.ParameterType]).ToArray())!;
        }

        public async ValueTask DisposeAsync()
        {
            await _scope.DisposeAsync();
            await _provider.DisposeAsync();
        }
    }

    private sealed class HostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "OnlineStore.Tests";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
