using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.Services;

namespace OnlineStore.Tests.Services;

public class DeliveryAddressServiceTests
{
    private static async Task<(ApplicationDbContext Context, ApplicationUser User)> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);
        var user = new ApplicationUser
        {
            Id = "user-1",
            UserName = "user@example.com",
            Email = "user@example.com",
            FirstName = "Hao",
            LastName = "Hu",
            PhoneNumber = "13800138000"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return (context, user);
    }

    private static DeliveryAddressInput Input(string line1, bool isDefault = false) =>
        new("Hu Hao", "13800138000", line1, null, "Leicester", "LE1 1AA", "UK", isDefault);

    [Fact]
    public async Task SaveAsync_AllowsMultipleAddresses_AndKeepsOneDefault()
    {
        var (context, user) = await CreateContextAsync();
        using var _ = context;
        var service = new DeliveryAddressService(context);

        var first = await service.SaveAsync(user.Id, null, Input("1 Castle Court"));
        var second = await service.SaveAsync(user.Id, null, Input("2 Market Square"));

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.True(first.IsDefault);
        Assert.False(second.IsDefault);
        Assert.Equal(2, await context.DeliveryAddresses.CountAsync());
        Assert.Equal(
            "1 Castle Court, Leicester, LE1 1AA, UK",
            (await context.Users.FindAsync(user.Id))!.Address);
    }

    [Fact]
    public async Task SetDefaultAsync_UpdatesDefaultAndLegacyCheckoutAddress()
    {
        var (context, user) = await CreateContextAsync();
        using var _ = context;
        var service = new DeliveryAddressService(context);
        var first = await service.SaveAsync(user.Id, null, Input("1 Castle Court"));
        var second = await service.SaveAsync(user.Id, null, Input("2 Market Square"));

        var updated = await service.SetDefaultAsync(user.Id, second!.Id);

        Assert.True(updated);
        Assert.False((await context.DeliveryAddresses.FindAsync(first!.Id))!.IsDefault);
        Assert.True((await context.DeliveryAddresses.FindAsync(second.Id))!.IsDefault);
        Assert.Equal(
            "2 Market Square, Leicester, LE1 1AA, UK",
            (await context.Users.FindAsync(user.Id))!.Address);
    }

    [Fact]
    public async Task DeleteAsync_RemovesDefaultAndPromotesRemainingAddress()
    {
        var (context, user) = await CreateContextAsync();
        using var _ = context;
        var service = new DeliveryAddressService(context);
        var first = await service.SaveAsync(user.Id, null, Input("1 Castle Court"));
        var second = await service.SaveAsync(user.Id, null, Input("2 Market Square"));

        Assert.True(await service.DeleteAsync(user.Id, first!.Id));

        var remaining = await context.DeliveryAddresses.SingleAsync();
        Assert.Equal(second!.Id, remaining.Id);
        Assert.True(remaining.IsDefault);
        Assert.Equal(
            "2 Market Square, Leicester, LE1 1AA, UK",
            (await context.Users.FindAsync(user.Id))!.Address);
    }

    [Fact]
    public async Task DeleteAsync_DoesNotAllowDeletingAnotherUsersAddress()
    {
        var (context, user) = await CreateContextAsync();
        using var _ = context;
        var service = new DeliveryAddressService(context);
        var address = await service.SaveAsync(user.Id, null, Input("1 Castle Court"));
        context.Users.Add(new ApplicationUser { Id = "user-2", UserName = "other@example.com" });
        await context.SaveChangesAsync();

        Assert.False(await service.DeleteAsync("user-2", address!.Id));
        Assert.Equal(1, await context.DeliveryAddresses.CountAsync());
    }

    [Fact]
    public async Task EnsureLegacyAddressAsync_ConvertsExistingSingleAddress()
    {
        var (context, user) = await CreateContextAsync();
        using var _ = context;
        user.Address = "10 High Street, Flat 2, London, SW1A 1AA, UK";
        await context.SaveChangesAsync();
        var service = new DeliveryAddressService(context);

        await service.EnsureLegacyAddressAsync(user);

        var address = await context.DeliveryAddresses.SingleAsync();
        Assert.Equal("HuHao", address.RecipientName);
        Assert.Equal("10 High Street", address.AddressLine1);
        Assert.Equal("London", address.City);
        Assert.True(address.IsDefault);
    }
}
