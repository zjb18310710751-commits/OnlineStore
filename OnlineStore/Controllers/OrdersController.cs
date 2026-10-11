using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineStore.Models;
using OnlineStore.Services;
using OnlineStore.ViewModels;

namespace OnlineStore.Controllers;

/// <summary>订单：结算、下单、订单确认与我的订单（需登录）。</summary>
[Authorize]
public class OrdersController : Controller
{
    private readonly IOrderService _orderService;
    private readonly ICartService _cartService;
    private readonly ICouponService _couponService;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrdersController(
        IOrderService orderService,
        ICartService cartService,
        ICouponService couponService,
        UserManager<ApplicationUser> userManager)
    {
        _orderService = orderService;
        _cartService = cartService;
        _couponService = couponService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var cart = await _cartService.GetCartAsync();
        if (cart.Items.Count == 0)
        {
            return RedirectToAction("Index", "Cart");
        }

        var user = await _userManager.GetUserAsync(User);
        var vm = new CheckoutViewModel
        {
            Items = cart.Items,
            Total = cart.Total,
            UserName = $"{user?.LastName}{user?.FirstName}".Trim(),
            Phone = user?.PhoneNumber ?? string.Empty,
            Address = user?.Address ?? string.Empty
        };
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

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        // 校验并应用优惠码
        decimal discount = 0;
        Coupon? applied = null;
        if (!string.IsNullOrWhiteSpace(vm.CouponCode))
        {
            var result = await _couponService.ValidateAsync(vm.CouponCode, cart.Total);
            if (!result.Success)
            {
                ModelState.AddModelError(nameof(vm.CouponCode), result.Error!);
                return View(vm);
            }
            discount = result.DiscountAmount;
            applied = result.Coupon;
        }

        var user = await _userManager.GetUserAsync(User);

        var order = new Order
        {
            OrderNo = GenerateOrderNo(),
            UserId = user?.Id,
            UserName = vm.UserName,
            Phone = vm.Phone,
            Address = vm.Address,
            Total = cart.Total - discount,
            CouponCode = applied?.Code,
            DiscountAmount = discount,
            Status = "待发货",
            Items = cart.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.Name,
                Color = i.Color,
                Spec = i.Spec,
                Price = i.Price,
                Quantity = i.Quantity
            }).ToList()
        };

        await _orderService.CreateAsync(order);
        if (applied is not null)
        {
            await _couponService.IncrementUsageAsync(applied.Id);
        }
        await _cartService.ClearAsync();

        return RedirectToAction(nameof(Confirmation), new { id = order.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Confirmation(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var order = await _orderService.GetByIdAsync(id);
        if (order is null || order.UserId != user?.Id)
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
        if (order is null || order.UserId != user?.Id)
        {
            return NotFound();
        }

        var ok = await _orderService.CancelAsync(id);
        TempData[ok ? "Success" : "Error"] = ok ? "订单已取消。" : "取消失败：当前状态不可取消。";
        return RedirectToAction(nameof(MyOrders));
    }

    private static string GenerateOrderNo() =>
        $"OS{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";
}
