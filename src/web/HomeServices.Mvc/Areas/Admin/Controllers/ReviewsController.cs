using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using HomeServices.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Mvc.Areas.Admin.Controllers;

/// <summary>
/// Admin review moderation: approve, reject or delete customer reviews before they
/// surface on expert profiles. Keeps a clean, trustworthy reputation system.
/// </summary>
public class ReviewsController : AdminControllerBase
{
    private readonly IReviewService _reviews;

    public ReviewsController(IReviewService reviews)
    {
        _reviews = reviews;
    }

    public async Task<IActionResult> Index(
        ReviewStatus? status = null, int? minRating = null, int page = 1, CancellationToken cancellationToken = default)
    {
        var filter = new ReviewFilterDto
        {
            Status = status,
            MinRating = minRating,
            Page = page,
            PageSize = 20,
        };

        var result = await _reviews.GetPagedAsync(filter, cancellationToken);
        ViewBag.CurrentFilter = filter;
        return View(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, ReviewStatus status, CancellationToken cancellationToken)
    {
        var ok = await _reviews.UpdateStatusAsync(id, status, cancellationToken);
        NotifySuccess(ok ? "وضعیت نظر تغییر کرد." : "عملیات ناموفق بود.");
        return RedirectToAction(nameof(Index), new { status = ViewBag.CurrentFilter?.Status });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var ok = await _reviews.DeleteAsync(id, cancellationToken);
        NotifySuccess(ok ? "نظر حذف شد." : "حذف ناموفق بود.");
        return RedirectToAction(nameof(Index));
    }
}
