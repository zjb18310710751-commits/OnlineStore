using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Localization;
using OnlineStore.Controllers;
using OnlineStore.Models;
using OnlineStore.Services;

namespace OnlineStore.Tests.Controllers;

public class CartControllerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Update_RejectsMissingOrNonPositiveQuantity_WithoutRemovingItem(int? quantity)
    {
        var cartService = new RecordingCartService();
        var controller = CreateController(cartService);

        var result = await controller.Update(1, null, quantity);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Null(cartService.UpdatedQuantity);
        Assert.Equal("请输入商品数量", controller.TempData["Error"]);
    }

    [Fact]
    public async Task Update_PassesValidQuantityToCartService()
    {
        var cartService = new RecordingCartService();
        var controller = CreateController(cartService);

        var result = await controller.Update(1, null, 3);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(3, cartService.UpdatedQuantity);
    }

    [Fact]
    public async Task Add_UsesLocalizedSuccessMessage()
    {
        var controller = new CartController(
            new RecordingCartService(),
            new TestStringLocalizer(new Dictionary<string, string>
            {
                ["CartAdded"] = "Added to cart."
            }))
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new NullTempDataProvider())
        };

        await controller.Add(1, 2);

        Assert.Equal("Added to cart.", controller.TempData["Success"]);
    }

    private static CartController CreateController(ICartService cartService) =>
        new(cartService, new TestStringLocalizer())
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new NullTempDataProvider())
        };

    private sealed class RecordingCartService : ICartService
    {
        public int? UpdatedQuantity { get; private set; }

        public Task<Cart> GetCartAsync() => Task.FromResult(new Cart());

        public Task AddAsync(int productId, int quantity = 1, int? skuId = null) => Task.CompletedTask;

        public Task UpdateAsync(int productId, int? skuId, int quantity)
        {
            UpdatedQuantity = quantity;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(int productId, int? skuId) => Task.CompletedTask;

        public Task ClearAsync() => Task.CompletedTask;
    }

    private sealed class NullTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) =>
            new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }

    private sealed class TestStringLocalizer : IStringLocalizer<SharedResource>
    {
        private readonly IReadOnlyDictionary<string, string> _values;

        public TestStringLocalizer(IReadOnlyDictionary<string, string>? values = null)
        {
            _values = values ?? new Dictionary<string, string>
            {
                ["CartQuantityRequired"] = "请输入商品数量",
                ["CartAdded"] = "已加入购物车。"
            };
        }

        public LocalizedString this[string name] =>
            new(name, _values.GetValueOrDefault(name, name));

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(_values.GetValueOrDefault(name, name), arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            _values.Select(item => new LocalizedString(item.Key, item.Value));
    }
}
