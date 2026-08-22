using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Mvc.Areas.Admin.Controllers;

/// <summary>
/// Site revenue dashboard (درآمد سایت). Site revenue equals the 10% commission
/// kept from every completed order's payout. Shows summary cards (today/week/
/// month/year/total), a dynamic chart fed by real database data, and the list of
/// commission records per completed order.
/// </summary>
public class RevenueController : AdminControllerBase
{
    private readonly IExpertPayoutService _payouts;
    private readonly ILogger<RevenueController> _logger;

    public RevenueController(IExpertPayoutService payouts, ILogger<RevenueController> logger)
    {
        _payouts = payouts;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string period = "daily", int page = 1, CancellationToken cancellationToken = default)
    {
        var summary = await _payouts.GetSiteRevenueSummaryAsync(period, cancellationToken);
        var payouts = await _payouts.GetPagedAsync(
            new ExpertPayoutFilterDto { Page = page, PageSize = 20 }, cancellationToken);

        ViewBag.Period = period;
        return View((summary, payouts));
    }
}
