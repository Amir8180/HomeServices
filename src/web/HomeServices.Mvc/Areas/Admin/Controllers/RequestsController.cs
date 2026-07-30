using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using HomeServices.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Mvc.Areas.Admin.Controllers;

/// <summary>
/// Admin request management: browse/filter every service request, drill into details
/// and force status transitions (override the customer/expert driven lifecycle).
/// </summary>
public class RequestsController : AdminControllerBase
{
    private readonly IServiceRequestService _requests;
    private readonly ICategoryService _categories;

    public RequestsController(IServiceRequestService requests, ICategoryService categories)
    {
        _requests = requests;
        _categories = categories;
    }

    public async Task<IActionResult> Index(
        int? categoryId = null, RequestStatus? status = null, UrgencyLevel? urgency = null,
        string? search = null, int page = 1, CancellationToken cancellationToken = default)
    {
        var filter = new ServiceRequestFilterDto
        {
            CategoryId = categoryId,
            Status = status,
            Urgency = urgency,
            SearchTerm = search,
            Page = page,
            PageSize = 20,
        };

        var result = await _requests.GetPagedAsync(filter, cancellationToken);
        ViewBag.Categories = await _categories.GetAllAsync(false, cancellationToken);
        ViewBag.CurrentFilter = filter;
        return View(result);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var request = await _requests.GetByIdAsync(id, cancellationToken);
        if (request == null) return NotFound();
        return View(request);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, RequestStatus status, CancellationToken cancellationToken)
    {
        var ok = await _requests.UpdateStatusAsync(id, status, cancellationToken);
        NotifySuccess(ok ? "وضعیت درخواست به‌روزرسانی شد." : "درخواست یافت نشد.");
        return RedirectToAction(nameof(Details), new { id });
    }
}
