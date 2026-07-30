using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Mvc.Areas.Admin.Controllers;

/// <summary>
/// Admin expert verification: review and approve/suspend professional profiles so
/// only vetted experts can quote on the platform.
/// </summary>
public class ExpertsController : AdminControllerBase
{
    private readonly IExpertProfileService _experts;

    public ExpertsController(IExpertProfileService experts)
    {
        _experts = experts;
    }

    public async Task<IActionResult> Index(
        bool? approved = null, string? search = null, int page = 1, CancellationToken cancellationToken = default)
    {
        var filter = new ExpertProfileFilterDto
        {
            IsApproved = approved,
            SearchTerm = search,
            ActiveOnly = false,
            Page = page,
            PageSize = 20,
        };

        var result = await _experts.GetPagedAsync(filter, cancellationToken);
        ViewBag.CurrentFilter = filter;
        return View(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleApproval(int id, CancellationToken cancellationToken)
    {
        var ok = await _experts.ApproveAsync(id, cancellationToken); // toggles IsApproved
        NotifySuccess(ok ? "وضعیت تأیید کارشناس تغییر کرد." : "عملیات ناموفق بود.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var ok = await _experts.DeleteAsync(id, cancellationToken);
        NotifySuccess(ok ? "پروفایل کارشناس حذف شد." : "حذف ناموفق بود.");
        return RedirectToAction(nameof(Index));
    }
}
