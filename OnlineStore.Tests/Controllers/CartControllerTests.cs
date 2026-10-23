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
        private static readonly IReadOnlyDictionary<string, string> Values =
            new Dictionary<string, string>
            {
                ["CartQuantityRequired"] = "请输入商品数量"
            };

        public LocalizedString this[string name] =>
            new(name, Values.GetValueOrDefault(name, name));

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(Values.GetValueOrDefault(name, name), arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Values.Select(item => new LocalizedString(item.Key, item.Value));
    }
}
