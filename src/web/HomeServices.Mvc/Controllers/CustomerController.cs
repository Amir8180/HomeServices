using System.Security.Claims;
using HomeServices.Application.Interfaces;
using HomeServices.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Mvc.Controllers;

[Authorize(Policy = "CustomerOnly")]
public class CustomerController : Controller
{
    private readonly IServiceRequestService _requests;
    private readonly IOrderService _orders;
    private readonly IReviewService _reviews;

    public CustomerController(
        IServiceRequestService requests,
        IOrderService orders,
        IReviewService reviews)
    {
        _requests = requests;
        _orders = orders;
        _reviews = reviews;
    }

    public async Task<IActionResult> Dashboard()
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var requests = await _requests.GetByCustomerAsync(userId.Value, HttpContext.RequestAborted);
        var orders = await _orders.GetByCustomerAsync(userId.Value, HttpContext.RequestAborted);

        ViewBag.OpenRequestsCount = requests.Count(r => r.Status == RequestStatus.Open || r.Status == RequestStatus.Quoted);
        ViewBag.BookedRequestsCount = requests.Count(r => r.Status == RequestStatus.Booked || r.Status == RequestStatus.InProgress);
        ViewBag.CompletedOrdersCount = orders.Count(o => o.Status == OrderStatus.Completed);
        ViewBag.ActiveOrdersCount = orders.Count(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled);

        return View();
    }

    public async Task<IActionResult> MyRequests()
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var requests = await _requests.GetByCustomerAsync(userId.Value, HttpContext.RequestAborted);
        return View(requests);
    }

    public async Task<IActionResult> MyOrders()
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var orders = await _orders.GetByCustomerAsync(userId.Value, HttpContext.RequestAborted);
        return View(orders);
    }

    private Guid? GetUserId()
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(id, out var guid) ? guid : null;
    }
}
