using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using HomeServices.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Mvc.Controllers;

[Authorize]
public class ProposalsController : Controller
{
    private readonly IProposalService _proposals;
    private readonly IServiceRequestService _requests;
    private readonly IOrderService _orders;
    private readonly IExpertProfileService _experts;
    private readonly ILogger<ProposalsController> _logger;

    public ProposalsController(
        IProposalService proposals,
        IServiceRequestService requests,
        IOrderService orders,
        IExpertProfileService experts,
        ILogger<ProposalsController> logger)
    {
        _proposals = proposals;
        _requests = requests;
        _orders = orders;
        _experts = experts;
        _logger = logger;
    }

    // -------------------- Compare proposals for a request --------------------
    [Authorize(Policy = "CustomerOnly")]
    public async Task<IActionResult> Compare(int requestId)
    {
        var request = await _requests.GetByIdAsync(requestId, HttpContext.RequestAborted);
        if (request == null) return NotFound();

        var userId = GetUserId();
        if (request.CustomerId != userId) return Forbid();

        var proposals = await _proposals.GetByRequestAsync(requestId, HttpContext.RequestAborted);

        // Enrich with expert profile info.
        var expertIds = proposals.Select(p => p.ExpertId).Distinct().ToList();
        var experts = new Dictionary<Guid, ExpertProfileDto?>();
        foreach (var eid in expertIds)
            experts[eid] = await _experts.GetByUserIdAsync(eid, HttpContext.RequestAborted);

        ViewBag.Request = request;
        ViewBag.Experts = experts;
        return View(proposals);
    }

    // -------------------- Accept a proposal -> creates an order --------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<IActionResult> Accept(int proposalId, int requestId)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var request = await _requests.GetByIdAsync(requestId, HttpContext.RequestAborted);
        if (request == null || request.CustomerId != userId) return Forbid();
        if (request.AcceptedProposalId.HasValue)
        {
            TempData["Error"] = "برای این درخواست از قبل پیشنهادی انتخاب شده است.";
            return RedirectToAction(nameof(Compare), new { requestId });
        }

        try
        {
            var accepted = await _proposals.AcceptAsync(proposalId, userId.Value, HttpContext.RequestAborted);
            if (!accepted)
            {
                TempData["Error"] = "انتخاب پیشنهاد ناموفق بود.";
                return RedirectToAction(nameof(Compare), new { requestId });
            }

            // Create the order from the accepted proposal.
            var order = await _orders.CreateFromProposalAsync(proposalId, userId.Value, HttpContext.RequestAborted);
            TempData["Success"] = "پیشنهاد انتخاب شد و سفارش شما ایجاد شد.";
            return RedirectToAction("Details", "Orders", new { id = order.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to accept proposal {ProposalId}.", proposalId);
            TempData["Error"] = "خطا در انتخاب پیشنهاد.";
            return RedirectToAction(nameof(Compare), new { requestId });
        }
    }

    private Guid? GetUserId()
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(id, out var guid) ? guid : null;
    }
}
