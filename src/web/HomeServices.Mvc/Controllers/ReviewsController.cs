using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Mvc.Controllers;

[Authorize]
public class ReviewsController : Controller
{
    private readonly IReviewService _reviews;
    private readonly IOrderService _orders;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(IReviewService reviews, IOrderService orders, ILogger<ReviewsController> logger)
    {
        _reviews = reviews;
        _orders = orders;
        _logger = logger;
    }

    // -------------------- Create review for a completed order --------------------
    [HttpGet]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<IActionResult> Create(int orderId)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var order = await _orders.GetByIdAsync(orderId, HttpContext.RequestAborted);
        if (order == null || order.CustomerId != userId) return Forbid();
        if (order.Status != Domain.Enums.OrderStatus.Completed)
        {
            TempData["Error"] = "فقط سفارش‌های تکمیل‌شده قابل ارزیابی هستند.";
            return RedirectToAction("Details", "Orders", new { id = orderId });
        }
        if (await _reviews.GetByOrderIdAsync(orderId, HttpContext.RequestAborted) != null)
        {
            TempData["Info"] = "نظر شما برای این سفارش قبلاً ثبت شده است.";
            return RedirectToAction("Details", "Orders", new { id = orderId });
        }

        var vm = new CreateReviewViewModel { OrderId = orderId };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<IActionResult> Create(CreateReviewViewModel vm)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");
        if (!ModelState.IsValid) return View(vm);

        var order = await _orders.GetByIdAsync(vm.OrderId, HttpContext.RequestAborted);
        if (order == null || order.CustomerId != userId) return Forbid();
        if (order.Status != Domain.Enums.OrderStatus.Completed)
        {
            TempData["Error"] = "فقط سفارش‌های تکمیل‌شده قابل ارزیابی هستند.";
            return RedirectToAction("Details", "Orders", new { id = vm.OrderId });
        }
        if (await _reviews.GetByOrderIdAsync(vm.OrderId, HttpContext.RequestAborted) != null)
        {
            TempData["Info"] = "نظر شما برای این سفارش قبلاً ثبت شده است.";
            return RedirectToAction("Details", "Orders", new { id = vm.OrderId });
        }

        try
        {
            var dto = new CreateReviewDto
            {
                OrderId = vm.OrderId,
                Rating = vm.Rating,
                Professionalism = vm.Professionalism,
                Punctuality = vm.Punctuality,
                Quality = vm.Quality,
                Responsiveness = vm.Responsiveness,
                Value = vm.Value,
                Comment = vm.Comment,
                ServiceDate = vm.ServiceDate,
            };
            await _reviews.CreateAsync(dto, userId.Value, HttpContext.RequestAborted);
            TempData["Success"] = "نظر شما ثبت شد و پس از تأیید مدیر، در پروفایل کارشناس نمایش داده خواهد شد. متشکریم!";
            return RedirectToAction("Details", "Orders", new { id = vm.OrderId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create review for order {OrderId}.", vm.OrderId);
            ModelState.AddModelError(string.Empty, "ثبت نظر ناموفق بود.");
            return View(vm);
        }
    }

    private Guid? GetUserId()
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(id, out var guid) ? guid : null;
    }
}

public class CreateReviewViewModel
{
    public int OrderId { get; set; }

    [Required(ErrorMessage = "لطفاً امتیاز کلی را ثبت کنید.")]
    [Range(1, 5, ErrorMessage = "امتیاز باید بین ۱ تا ۵ باشد.")]
    public int Rating { get; set; }

    [Range(1, 5)] public int? Professionalism { get; set; }
    [Range(1, 5)] public int? Punctuality { get; set; }
    [Range(1, 5)] public int? Quality { get; set; }
    [Range(1, 5)] public int? Responsiveness { get; set; }
    [Range(1, 5)] public int? Value { get; set; }

    [StringLength(2000)] public string? Comment { get; set; }
    [DataType(DataType.Date)] public DateTime? ServiceDate { get; set; }
}
