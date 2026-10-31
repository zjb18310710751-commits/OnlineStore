using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.Services;

namespace OnlineStore.Tests.Services;

public class OrderShippingSnapshotTests
{
    [Fact]
    public async Task CreateAsync_LongMultiProductShippingTextFitsEverySnapshotColumn()
    {
        await using var context = CreateContext();
        var first = Product(1, '甲', 'A');
        var second = Product(2, '乙', 'B');
        context.Products.AddRange(first, second);
        await context.SaveChangesAsync();

        var order = await new OrderService(context).CreateAsync(OrderFor(first, second));

        AssertSnapshot(order, nameof(Order.ShippingOrigin), first.ShippingFrom, second.ShippingFrom);
        AssertSnapshot(order, nameof(Order.ShippingOriginEn), first.ShippingFromEn, second.ShippingFromEn);
        AssertSnapshot(order, nameof(Order.ShippingFeeText), first.ShippingFee, second.ShippingFee);
        AssertSnapshot(order, nameof(Order.ShippingFeeTextEn), first.ShippingFeeEn, second.ShippingFeeEn);
        AssertSnapshot(order, nameof(Order.DispatchTimeText), first.DeliveryTime, second.DeliveryTime);
        AssertSnapshot(order, nameof(Order.DispatchTimeTextEn), first.DeliveryTimeEn, second.DeliveryTimeEn);
        Assert.Equal(20m, order.Total);
        Assert.Equal(9, first.Stock);
        Assert.Equal(9, second.Stock);
    }

    [Fact]
    public async Task CreateAsync_TruncationDoesNotSplitUnicodeSurrogatePair()
    {
        await using var context = CreateContext();
        var first = Product(1, '甲', 'A');
        var second = Product(2, '乙', 'B');
        second.ShippingFrom = new string('乙', 97) + "🚚尾";
        context.Products.AddRange(first, second);
        await context.SaveChangesAsync();

        var order = await new OrderService(context).CreateAsync(OrderFor(first, second));

        Assert.Equal(first.ShippingFrom + "、" + new string('乙', 97) + "…", order.ShippingOrigin);
        Assert.False(char.IsSurrogate(order.ShippingOrigin![^2]));
    }

    private static void AssertSnapshot(Order order, string propertyName, params string?[] parts)
    {
        var property = typeof(Order).GetProperty(propertyName)!;
        var limit = property.GetCustomAttribute<StringLengthAttribute>()!.MaximumLength;
        var actual = Assert.IsType<string>(property.GetValue(order));
        var combined = string.Join("、", parts);
        Assert.True(actual.Length <= limit, $"{propertyName}: {actual.Length} exceeds {limit}");
        Assert.EndsWith("…", actual);
        Assert.Equal(combined[..(actual.Length - 1)] + "…", actual);
    }

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static Product Product(int id, char chinese, char english) => new()
    {
        Id = id, Name = $"Product {id}", Price = 10m, Stock = 10, ShopId = 1,
        ShippingFrom = new string(chinese, 100), ShippingFromEn = new string(english, 100),
        ShippingFee = new string(chinese, 50), ShippingFeeEn = new string(english, 50),
        DeliveryTime = "24" + new string(chinese, 98), DeliveryTimeEn = "24" + new string(english, 98)
    };

    private static Order OrderFor(params Product[] products) => new()
    {
        OrderNo = "SHIPPING-SNAPSHOT",
        UserId = "buyer",
        Items = products.Select(product => new OrderItem
        {
            ProductId = product.Id, ProductName = product.Name, Price = product.Price, Quantity = 1
        }).ToList()
    };
}
