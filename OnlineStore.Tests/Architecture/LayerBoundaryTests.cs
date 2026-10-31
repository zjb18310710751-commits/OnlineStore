using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OnlineStore;
using OnlineStore.Controllers;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.Services;

namespace OnlineStore.Tests.Architecture;

public class LayerBoundaryTests
{
    [Fact]
    public void BusinessDataAndModels_AreIndependentOfTheWebAssembly()
    {
        var web = typeof(HomeController).Assembly;
        var business = typeof(CartService).Assembly;
        var data = typeof(ApplicationDbContext).Assembly;
        var models = typeof(Product).Assembly;

        Assert.Equal("OnlineStore", web.GetName().Name);
        Assert.Equal("OnlineStore.Business", business.GetName().Name);
        Assert.Equal("OnlineStore.DataAccess", data.GetName().Name);
        Assert.Equal("OnlineStore.Models", models.GetName().Name);
        Assert.Same(models, typeof(SharedResource).Assembly);
        Assert.DoesNotContain(business.GetReferencedAssemblies(), dependency => dependency.Name == web.GetName().Name);
        Assert.DoesNotContain(data.GetReferencedAssemblies(),
            dependency => dependency.Name == web.GetName().Name || dependency.Name == business.GetName().Name);
        Assert.DoesNotContain(models.GetReferencedAssemblies(), dependency =>
            dependency.Name == web.GetName().Name || dependency.Name == data.GetName().Name
            || dependency.Name == business.GetName().Name);
    }

    [Theory]
    [InlineData("zh-CN", "首页", "已加入购物车。")]
    [InlineData("en", "Home", "Added to cart.")]
    public void SharedResources_AreResolvedFromTheirOwnAssembly(string culture, string home, string addedToCart)
    {
        var previousCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
        try
        {
            using var services = new ServiceCollection().AddLogging()
                .AddLocalization(options => options.ResourcesPath = "Resources").BuildServiceProvider();
            var localizer = services.GetRequiredService<IStringLocalizer<SharedResource>>();

            Assert.False(localizer["Home"].ResourceNotFound);
            Assert.Equal(home, localizer["Home"].Value);
            Assert.Equal(addedToCart, localizer["AddedToCart"].Value);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }
    }

    [Fact]
    public void DatabaseContext_DiscoversExistingMigrationsAndSnapshotWithoutConnecting()
    {
        using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=unused;Database=ArchitectureVerification;Integrated Security=True")
            .Options);
        var existingIds = new[]
        {
            "20261020140733_InitialCreate", "20261023234500_AddUserAvatar",
            "20261024090000_AddCategoryDescriptionEn", "20261024103000_AddDeliveryAddressBook",
            "20261025170000_AddHeroSlides", "20261025180000_AddOrderItemSku",
            "20261026001008_AddCouponRules", "20261026004942_AddOrderCouponName",
            "20261026162808_AddOrderFulfillmentAndAfterSales", "20261026170416_AddOrderUserDeletion",
            "20261026172851_AddOrderShippingSnapshot", "20261026182849_AddReviewOrderLink",
            "20261026200000_SetUkProductShippingOrigins", "20261026222048_FixInventoryCheckoutAndOrderConstraints",
            "20261026231833_AddShopLogoUrl", "20261028213537_AddReceiptConfirmationWorkflow",
            "20261028231500_AddReturnShipmentTracking", "20261029010511_AddVerificationCodeAttempts"
        };

        var migrations = context.Database.GetMigrations().ToArray();

        Assert.All(existingIds, id => Assert.Contains(id, migrations));
        Assert.NotNull(context.GetService<IMigrationsAssembly>().ModelSnapshot);
        Assert.Same(typeof(ApplicationDbContext).Assembly, context.GetService<IMigrationsAssembly>().Assembly);
    }
}
