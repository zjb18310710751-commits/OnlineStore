using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using OnlineStore.Services;

namespace OnlineStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OrdersController : Controller
{
    private readonly IOrderService _orderService;
    private readonly IStringLocalizer<SharedResource> _loc;

    public OrdersController(IOrderService orderService, IStringLocalizer<SharedResource> localizer)
    {
        _orderService = orderService;
        _loc = localizer;
    }

    public async Task<IActionResult> Index()
    {
        var orders = await _orderService.GetAllAsync();
        return View(orders);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ship(int id)
    {
        var ok = await _orderService.ShipAsync(id);
        TempData[ok ? "Success" : "Error"] = ok ? _loc["OrderShipped"].Value : _loc["OrderShipFailed"].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var ok = await _orderService.CompleteAsync(id);
        TempData[ok ? "Success" : "Error"] = ok ? _loc["OrderCompleted"].Value : _loc["OrderCompleteFailed"].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var ok = await _orderService.CancelAsync(id);
        TempData[ok ? "Success" : "Error"] = ok ? _loc["OrderCancelledRestored"].Value : _loc["OrderCancelFailedAdmin"].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Refund(int id)
    {
        var ok = await _orderService.RefundAsync(id);
        TempData[ok ? "Success" : "Error"] = ok ? _loc["OrderRefunded"].Value : _loc["OrderRefundFailed"].Value;
        return RedirectToAction(nameof(Index));
    }
}
