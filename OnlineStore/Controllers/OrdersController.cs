using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Mvc;
using OnlineStore.Models;
using OnlineStore.Services;
using OnlineStore.ViewModels;

namespace OnlineStore.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly IOrderService _orderService;
    private readonly ICartService _cartService;
    private readonly ICouponService _couponService;
    private readonly IDeliveryAddressService _deliveryAddressService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStringLocalizer<SharedResource> _loc;

    public OrdersController(
        IOrderService orderService,
        ICartService cartService,
        ICouponService couponService,
        IDeliveryAddressService deliveryAddressService,
        UserManager<ApplicationUser> userManager,
        IStringLocalizer<SharedResource> localizer)
    {
        _orderService = orderService;
        _cartService = cartService;
        _couponService = couponService;
        _deliveryAddressService = deliveryAddressService;
        _userManager = userManager;
        _loc = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Checkout(string? couponCode = null, int? couponId = null)
    {
        var cart = await _cartService.GetCartAsync();
        if (cart.Items.Count == 0)
        {
            return RedirectToAction("Index", "Cart");
        }

        var user = await _userManager.GetUserAsync(User);
        DeliveryAddress? defaultAddress = null;
        IReadOnlyList<DeliveryAddress> savedAddresses = Array.Empty<DeliveryAddress>();
        if (user is not null)
        {
            await _deliveryAddressService.EnsureLegacyAddressAsync(user);
            savedAddresses = await _deliveryAddressService.GetAsync(user.Id);
            defaultAddress = await _deliveryAddressService.GetDefaultAsync(user.Id);
        }

        var vm = new CheckoutViewModel
        {
            Items = cart.Items,
            Total = cart.Total,
            AddressId = defaultAddress?.Id,
            HasSavedAddresses = savedAddresses.Count > 0,
            UserName = defaultAddress?.RecipientName ?? $"{user?.LastName}{user?.FirstName}".Trim(),
            Phone = defaultAddress?.PhoneNumber ?? user?.PhoneNumber ?? string.Empty,
            Address = defaultAddress?.AddressLine1 ?? user?.Address ?? string.Empty,
            AddressLine2 = defaultAddress?.AddressLine2,
            City = defaultAddress?.City ?? string.Empty,
            PostalCode = defaultAddress?.PostalCode ?? string.Empty,
            Country = defaultAddress?.Country ?? string.Empty
        };
        if (user is not null)
        {
            if (string.IsNullOrWhiteSpace(couponCode) && couponId.HasValue)
            {
                couponCode = (await _couponService.GetByIdAsync(couponId.Value))?.Code;
            }
            var availableCoupons = await _couponService.GetAvailableAsync(
                user.Id,
                cart.Total,
                ToCouponEvaluationItems(cart.Items));
            if (string.IsNullOrWhiteSpace(couponCode))
            {
                ApplyBestCoupon(vm, availableCoupons);
            }
            else
            {
                var requested = await _couponService.ValidateAsync(
                    couponCode,
                    cart.Total,
                    user.Id,
                    ToCouponEvaluationItems(cart.Items));
                if (requested.Success && requested.Coupon is not null)
                {
                    vm.CouponCode = requested.Coupon.Code;
                    vm.DiscountAmount = requested.DiscountAmount;
                    vm.FreeShippingCoupon = requested.FreeShipping;
                    ApplyCouponChoices(vm, availableCoupons, requested.Coupon.Code);
                }
                else
                {
                    ModelState.AddModelError(nameof(vm.CouponCode), requested.Error ?? _loc["CouponUnavailable"].Value);
                    ApplyCouponChoices(vm, availableCoupons, null);
                }
            }
        }
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel vm)
    {
        var cart = await _cartService.GetCartAsync();
        if (cart.Items.Count == 0)
        {
            return RedirectToAction("Index", "Cart");
        }

        vm.Items = cart.Items;
        vm.Total = cart.Total;
        vm.DiscountAmount = 0;

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }
        await _deliveryAddressService.EnsureLegacyAddressAsync(user);
        vm.HasSavedAddresses = (await _deliveryAddressService.GetAsync(user.Id)).Count > 0;

        ValidateCheckoutDetails(vm);
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        decimal discount = 0;
        Coupon? applied = null;
        var freeShipping = false;
        if (!string.IsNullOrWhiteSpace(vm.CouponCode))
        {
            var result = await _couponService.ValidateAsync(
                vm.CouponCode,
                cart.Total,
                user.Id,
                ToCouponEvaluationItems(cart.Items));
            if (!result.Success)
            {
                ModelState.AddModelError(nameof(vm.CouponCode), result.Error!);
                await PopulateAvailableCouponsAsync(vm, user.Id);
                return View(vm);
            }
            discount = result.DiscountAmount;
            vm.DiscountAmount = discount;
            vm.FreeShippingCoupon = result.FreeShipping;
            applied = result.Coupon;
            freeShipping = result.FreeShipping;
        }

        if (vm.SaveAsDefaultAddress)
        {
            var savedAddress = await _deliveryAddressService.SaveAsync(
                user.Id,
                vm.AddressId,
                new DeliveryAddressInput(
                    vm.UserName,
                    vm.Phone,
                    vm.Address,
                    vm.AddressLine2,
                    vm.City,
                    vm.PostalCode,
                    vm.Country,
                    IsDefault: true));
            if (savedAddress is null)
            {
                ModelState.AddModelError(string.Empty, _loc["AddressSaveFailed"].Value);
                return View(vm);
            }
        }

        var order = new Order
        {
            OrderNo = GenerateOrderNo(),
            UserId = user.Id,
            UserName = vm.UserName!.Trim(),
            Phone = vm.Phone?.Trim(),
            Address = BuildFullAddress(vm),
            Total = cart.Total - discount,
            CouponCode = applied?.Code,
            DiscountAmount = discount,
            Status = OrderStatus.PendingShipment,
            Items = cart.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                SkuId = i.SkuId,
                ProductName = i.Name,
                ProductNameEn = i.NameEn,
                Color = i.Color,
                ColorEn = i.ColorEn,
                Spec = i.Spec,
                SpecEn = i.SpecEn,
                Price = i.Price,
                Quantity = i.Quantity
            }).ToList()
        };

        try
        {
            await _orderService.CreateAsync(order);
        }
        catch (OrderValidationException exception)
        {
            var refreshedCart = await _cartService.RefreshAsync();
            vm.Items = refreshedCart.Items;
            vm.Total = refreshedCart.Total;
            vm.DiscountAmount = 0;
            if (!string.IsNullOrWhiteSpace(vm.CouponCode) && refreshedCart.Items.Count > 0)
            {
                var refreshedCoupon = await _couponService.ValidateAsync(
                    vm.CouponCode,
                    refreshedCart.Total,
                    user.Id,
                    ToCouponEvaluationItems(refreshedCart.Items));
                if (refreshedCoupon.Success)
                {
                    vm.DiscountAmount = refreshedCoupon.DiscountAmount;
                    vm.FreeShippingCoupon = refreshedCoupon.FreeShipping;
                }
            }
            await PopulateAvailableCouponsAsync(vm, user.Id);
            ModelState.AddModelError(string.Empty, _loc[exception.ResourceKey].Value);
            return View(vm);
        }
        await _cartService.ClearAsync();

        return RedirectToAction(nameof(Confirmation), new { id = order.Id });
    }

    [HttpGet]
    public async Task<IActionResult> PreviewCoupon(string? couponCode)
    {
        var cart = await _cartService.GetCartAsync();
        if (cart.Items.Count == 0)
        {
            return Json(new { ok = false, error = _loc["EmptyCart"].Value });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var items = ToCouponEvaluationItems(cart.Items);
        if (string.IsNullOrWhiteSpace(couponCode))
        {
            var available = await _couponService.GetAvailableAsync(user.Id, cart.Total, items);
            var best = available.FirstOrDefault();
            return Json(new
            {
                ok = true,
                discount = best?.DiscountAmount ?? 0m,
                freeShipping = best?.FreeShipping ?? false,
                code = best?.Coupon?.Code,
                total = cart.Total,
                payable = cart.Total - (best?.DiscountAmount ?? 0m)
            });
        }

        var result = await _couponService.ValidateAsync(couponCode, cart.Total, user.Id, items);
        if (!result.Success)
        {
            return Json(new { ok = false, error = result.Error });
        }

        return Json(new
        {
            ok = true,
            discount = result.DiscountAmount,
            freeShipping = result.FreeShipping,
            code = result.Coupon?.Code,
            total = cart.Total,
            payable = cart.Total - result.DiscountAmount
        });
    }

    [HttpGet]
    public async Task<IActionResult> Confirmation(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var order = await _orderService.GetByIdAsync(id);
        if (user is null || order is null || order.UserId != user.Id)
        {
            return NotFound();
        }

        return View(order);
    }

    [HttpGet]
    public async Task<IActionResult> MyOrders()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var orders = await _orderService.GetByUserAsync(user.Id);
        return View(orders);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var order = await _orderService.GetByIdAsync(id);
        if (user is null || order is null || order.UserId != user.Id)
        {
            return NotFound();
        }

        var ok = await _orderService.CancelAsync(id);
        TempData[ok ? "Success" : "Error"] = ok ? _loc["OrderCancelled"].Value : _loc["OrderCancelFailed"].Value;
        return RedirectToAction(nameof(MyOrders));
    }

    [HttpGet]
    public async Task<IActionResult> GetOrderTracking(int orderId)
    {
        var user = await _userManager.GetUserAsync(User);
        var order = await _orderService.GetByIdAsync(orderId);
        if (user is null || order is null || order.UserId != user.Id)
        {
            return Json(new { success = false, message = L10n.Text("订单不存在。", "Order not found.") });
        }

        var tracking = await _orderService.GetTrackingAsync(orderId);
        if (tracking is null)
        {
            return Json(new { success = false, message = L10n.Text("物流信息尚未生成。", "Tracking is not available yet.") });
        }

        return Json(new { success = true, tracking });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmReceipt(int orderId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Json(new { success = false, message = _loc["AccessDenied"].Value });
        }

        var ok = await _orderService.ConfirmReceiptAsync(orderId, user.Id);
        return Json(new
        {
            success = ok,
            message = ok
                ? L10n.Text("确认收货成功，订单已完成。", "Receipt confirmed. Order completed.")
                : L10n.Text("当前订单无法确认收货。", "This order cannot be confirmed yet.")
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestAfterSales(int orderId, string type, string reason)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = L10n.Text("请填写售后原因。", "Please enter a reason.");
            return RedirectToAction(nameof(MyOrders));
        }

        try
        {
            await _orderService.CreateAfterSalesAsync(orderId, user.Id, type, reason);
            TempData["Success"] = L10n.Text("售后申请已提交，等待审核。", "After-sales request submitted.");
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(MyOrders));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var ok = await _orderService.DeleteForUserAsync(id, user.Id);
        TempData[ok ? "Success" : "Error"] = ok
            ? L10n.Text("订单已从你的订单列表中删除。", "The order has been removed from your order list.")
            : L10n.Text("当前订单不能删除。", "This order cannot be deleted.");
        return RedirectToAction(nameof(MyOrders));
    }

    private static string GenerateOrderNo() =>
        $"OS{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";

    private void ValidateCheckoutDetails(CheckoutViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(vm.UserName))
        {
            ModelState.AddModelError(nameof(vm.UserName), _loc["CheckoutReceiverRequired"]);
        }
        else if (vm.UserName.Length > 50)
        {
            ModelState.AddModelError(nameof(vm.UserName), _loc["CheckoutReceiverTooLong"]);
        }

        if (string.IsNullOrWhiteSpace(vm.Phone))
        {
            ModelState.AddModelError(nameof(vm.Phone), _loc["CheckoutPhoneRequired"]);
        }
        else if (vm.Phone.Length > 20)
        {
            ModelState.AddModelError(nameof(vm.Phone), _loc["CheckoutPhoneTooLong"]);
        }

        var fullAddress = BuildFullAddress(vm);
        if (string.IsNullOrWhiteSpace(vm.Address))
        {
            ModelState.AddModelError(nameof(vm.Address), _loc["CheckoutAddressRequired"]);
        }
        else if (fullAddress.Length > 500)
        {
            ModelState.AddModelError(nameof(vm.Address), _loc["CheckoutAddressTooLong"]);
        }

        if (string.IsNullOrWhiteSpace(vm.City))
        {
            ModelState.AddModelError(nameof(vm.City), _loc["CheckoutCityRequired"]);
        }

        if (string.IsNullOrWhiteSpace(vm.PostalCode))
        {
            ModelState.AddModelError(nameof(vm.PostalCode), _loc["CheckoutPostalCodeRequired"]);
        }

        if (string.IsNullOrWhiteSpace(vm.Country))
        {
            ModelState.AddModelError(nameof(vm.Country), _loc["CheckoutCountryRequired"]);
        }
    }

    private static string BuildFullAddress(CheckoutViewModel vm) =>
        AddressFormatter.Combine(
            vm.Address,
            vm.AddressLine2,
            vm.City,
            vm.PostalCode,
            vm.Country);

    private static IReadOnlyList<CouponEvaluationItem> ToCouponEvaluationItems(IEnumerable<CartItem> items) =>
        items
            .Select(item => new CouponEvaluationItem(item.ProductId, item.Price, item.Quantity))
            .ToList();

    private async Task PopulateAvailableCouponsAsync(CheckoutViewModel vm, string userId)
    {
        var available = await _couponService.GetAvailableAsync(
            userId,
            vm.Total,
            ToCouponEvaluationItems(vm.Items));
        ApplyCouponChoices(vm, available, vm.CouponCode);
    }

    private void ApplyBestCoupon(CheckoutViewModel vm, IReadOnlyList<CouponApplyResult> available)
    {
        var best = available.FirstOrDefault();
        if (best?.Coupon is null)
        {
            ApplyCouponChoices(vm, available, null);
            return;
        }

        vm.CouponCode = best.Coupon.Code;
        vm.DiscountAmount = best.DiscountAmount;
        vm.FreeShippingCoupon = best.FreeShipping;
        ApplyCouponChoices(vm, available, best.Coupon.Code);
    }

    private void ApplyCouponChoices(
        CheckoutViewModel vm,
        IReadOnlyList<CouponApplyResult> available,
        string? selectedCode)
    {
        var normalized = selectedCode?.Trim().ToUpperInvariant();
        vm.AvailableCoupons = available
            .Where(result => result.Coupon is not null)
            .Select((result, index) =>
            {
                var coupon = result.Coupon!;
                return new CouponChoiceViewModel
                {
                    Code = coupon.Code,
                    CampaignName = coupon.CampaignName,
                    DiscountType = coupon.DiscountType,
                    DiscountValue = coupon.DiscountValue,
                    CalculatedDiscount = result.DiscountAmount,
                    FreeShipping = result.FreeShipping,
                    EndDate = coupon.EndDate,
                    IsBest = index == 0,
                    IsSelected = coupon.Code == normalized
                };
            })
            .ToList();
    }
}
