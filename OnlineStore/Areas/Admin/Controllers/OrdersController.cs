using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using OnlineStore.Models;
using OnlineStore.Services;
using OnlineStore.ViewModels;

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

    public async Task<IActionResult> Index(string? section = null)
    {
        var orders = await _orderService.GetAllAsync();
        var afterSales = await _orderService.GetAfterSalesAllAsync();
        return View(new AdminOrdersViewModel
        {
            ActiveSection = string.Equals(section, "after-sales", StringComparison.OrdinalIgnoreCase)
                ? "after-sales"
                : "orders",
            Orders = orders,
            AfterSales = afterSales
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ship(int id, string carrier, string trackingNumber)
    {
        var ok = await _orderService.ShipAsync(id, carrier, trackingNumber);
        TempData[ok ? "Success" : "Error"] = ok ? _loc["OrderShipped"].Value : _loc["OrderShipFailed"].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> OrderTracking(int id)
    {
        var tracking = await _orderService.GetTrackingAsync(id);
        if (tracking is null)
        {
            return Json(new { success = false, message = L10n.Text("物流信息尚未生成。", "Tracking is not available yet.") });
        }

        return Json(new { success = true, tracking });
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewAfterSales(int id, bool approved, string? note)
    {
        var ok = await _orderService.ReviewAfterSalesAsync(id, approved, note);
        TempData[ok ? "Success" : "Error"] = ok
            ? L10n.Text("售后申请已处理。", "After-sales request reviewed.")
            : L10n.Text("售后申请处理失败。", "Could not review the request.");
        return RedirectToAction(nameof(Index), new { section = "after-sales" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAfterSalesReceived(int id)
    {
        var ok = await _orderService.MarkAfterSalesReceivedAsync(id);
        TempData[ok ? "Success" : "Error"] = ok
            ? L10n.Text("已标记为收到退回商品。", "Returned item received.")
            : L10n.Text("操作失败。", "Operation failed.");
        return RedirectToAction(nameof(Index), new { section = "after-sales" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteAfterSales(int id)
    {
        var ok = await _orderService.CompleteAfterSalesAsync(id);
        TempData[ok ? "Success" : "Error"] = ok
            ? L10n.Text("售后已完成。", "After-sales completed.")
            : L10n.Text("操作失败。", "Operation failed.");
        return RedirectToAction(nameof(Index), new { section = "after-sales" });
    }
}
