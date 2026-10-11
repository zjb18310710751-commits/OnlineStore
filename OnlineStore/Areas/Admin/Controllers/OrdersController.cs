using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineStore.Services;

namespace OnlineStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OrdersController : Controller
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
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
        TempData[ok ? "Success" : "Error"] = ok ? "订单已发货。" : "操作失败：订单状态不允许发货。";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var ok = await _orderService.CompleteAsync(id);
        TempData[ok ? "Success" : "Error"] = ok ? "订单已完成。" : "操作失败：订单状态不允许完成。";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var ok = await _orderService.CancelAsync(id);
        TempData[ok ? "Success" : "Error"] = ok ? "订单已取消，库存已恢复。" : "操作失败：订单状态不允许取消。";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Refund(int id)
    {
        var ok = await _orderService.RefundAsync(id);
        TempData[ok ? "Success" : "Error"] = ok ? "订单已退款，库存已恢复。" : "操作失败：订单状态不允许退款。";
        return RedirectToAction(nameof(Index));
    }
}
