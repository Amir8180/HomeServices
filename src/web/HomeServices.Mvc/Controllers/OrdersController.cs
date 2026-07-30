using System.Security.Claims;
using HomeServices.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Mvc.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly IOrderService _orders;
    private readonly IServiceRequestService _requests;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orders, IServiceRequestService requests, ILogger<OrdersController> logger)
    {
        _orders = orders;
        _requests = requests;
        _logger = logger;
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _orders.GetByIdAsync(id, HttpContext.RequestAborted);
        if (order == null) return NotFound();

        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        // Only the customer, the expert, or an admin can view.
        if (order.CustomerId != userId && order.ExpertId != userId && !User.IsInRole("Admin"))
            return Forbid();

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<IActionResult> Pay(int id)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var order = await _orders.GetByIdAsync(id, HttpContext.RequestAborted);
        if (order == null || order.CustomerId != userId) return Forbid();
        if (order.Status != Domain.Enums.OrderStatus.PendingPayment)
        {
            TempData["Error"] = "این سفارش قابل پرداخت نیست.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Mark as Paid (mock gateway).
        await _orders.UpdateStatusAsync(id, Domain.Enums.OrderStatus.Paid, HttpContext.RequestAborted);
        TempData["Success"] = "پرداخت با موفقیت انجام شد. (درگاه نمایشی)";
        return RedirectToAction(nameof(Details), new { id });
    }

    private Guid? GetUserId()
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(id, out var guid) ? guid : null;
    }
}
