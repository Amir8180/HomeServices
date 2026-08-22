using System.Security.Claims;
using HomeServices.Application.Contracts;
using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using HomeServices.Domain.Enums;
using HomeServices.Mvc.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Mvc.Areas.Admin.Controllers;

/// <summary>
/// Support dashboard with three sheets:
///  1) Finance (امور مالی)   — card-to-card payment reports awaiting verification.
///  2) Process (نظارت بر فرآیندها) — work-completion declarations to mediate.
///  3) Tickets (درخواست‌های پشتیبانی) — user-submitted help-desk tickets with a
///     message thread, attachments and status management.
/// </summary>
public class SupportController : AdminControllerBase
{
    private readonly IPaymentVerificationService _paymentReports;
    private readonly IWorkCompletionService _completions;
    private readonly ISupportTicketService _tickets;
    private readonly IIdentityApiClient _identity;
    private readonly ILogger<SupportController> _logger;

    public SupportController(
        IPaymentVerificationService paymentReports,
        IWorkCompletionService completions,
        ISupportTicketService tickets,
        IIdentityApiClient identity,
        ILogger<SupportController> logger)
    {
        _paymentReports = paymentReports;
        _completions = completions;
        _tickets = tickets;
        _identity = identity;
        _logger = logger;
    }

    // -------------------- Index (three tabs) --------------------
    public async Task<IActionResult> Index(
        string tab = "finance",
        PaymentVerificationStatus? payStatus = null,
        CompletionReviewStatus? completionStatus = null,
        SupportTicketStatus? ticketStatus = null,
        string? search = null,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        tab = tab is "process" or "tickets" ? tab : "finance";
        ViewBag.Tab = tab;
        ViewBag.SearchTerm = search;

        if (tab == "process")
        {
            var filter = new CompletionReviewFilterDto
            {
                Status = completionStatus,
                SearchTerm = search,
                Page = page,
                PageSize = 20,
            };
            var result = await _completions.GetPagedAsync(filter, cancellationToken);
            ViewBag.Completions = result;
            ViewBag.CompletionStatus = completionStatus;
            ViewBag.TotalCount = result.TotalCount;
            ViewBag.Page = result.PageNumber;
            ViewBag.PageSize = result.PageSize;
        }
        else if (tab == "tickets")
        {
            var filter = new SupportTicketFilterDto
            {
                Status = ticketStatus,
                SearchTerm = search,
                Page = page,
                PageSize = 20,
            };
            var result = await _tickets.GetPagedAsync(filter, cancellationToken);
            ViewBag.Tickets = result;
            ViewBag.TicketStatus = ticketStatus;
            ViewBag.TotalCount = result.TotalCount;
            ViewBag.Page = result.PageNumber;
            ViewBag.PageSize = result.PageSize;

            // Resolve submitter names from the Identity service.
            var names = new Dictionary<Guid, string>();
            foreach (var t in result.Items.Select(t => t.UserId).Distinct())
            {
                var user = await _identity.GetUserByIdAsync(t, cancellationToken);
                names[t] = user?.FullName ?? "—";
            }
            ViewBag.UserNames = names;

            // Counters for the status chips.
            var allTickets = await _tickets.GetPagedAsync(new SupportTicketFilterDto { PageSize = 100 }, cancellationToken);
            ViewBag.TicketCounts = allTickets.Items.GroupBy(t => t.Status).ToDictionary(g => g.Key, g => g.Count());
        }
        else
        {
            var filter = new PaymentVerificationFilterDto
            {
                Status = payStatus,
                SearchTerm = search,
                Page = page,
                PageSize = 20,
            };
            var result = await _paymentReports.GetPagedAsync(filter, cancellationToken);
            ViewBag.Payments = result;
            ViewBag.PaymentStatus = payStatus;
            ViewBag.TotalCount = result.TotalCount;
            ViewBag.Page = result.PageNumber;
            ViewBag.PageSize = result.PageSize;

            // Resolve customer names from the Identity service.
            var names = new Dictionary<Guid, string>();
            foreach (var customerId in result.Items.Select(r => r.CustomerId).Distinct())
            {
                var user = await _identity.GetUserByIdAsync(customerId, cancellationToken);
                names[customerId] = user?.FullName ?? "—";
            }
            ViewBag.CustomerNames = names;
        }

        return View();
    }

    // -------------------- Finance actions --------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyPayment(int id, string? note, CancellationToken cancellationToken)
    {
        var adminId = GetAdminId();
        var ok = await _paymentReports.VerifyAsync(id, note, adminId, cancellationToken);
        TempData[ok ? "Success" : "Error"] = ok
            ? "پرداخت تأیید شد؛ سفارش به حالت «پرداخت‌شده» تغییر کرد و به کارشناس اطلاع داده می‌شود."
            : "تأیید پرداخت ناموفق بود (رکورد یافت نشد یا قبلاً بررسی شده است).";
        return RedirectToAction(nameof(Index), new { tab = "finance" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectPayment(int id, string note, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            TempData["Error"] = "برای رد پرداخت، ثبت توضیحات الزامی است.";
            return RedirectToAction(nameof(Index), new { tab = "finance" });
        }

        var adminId = GetAdminId();
        var ok = await _paymentReports.RejectAsync(id, note, adminId, cancellationToken);
        TempData[ok ? "Success" : "Error"] = ok
            ? "پرداخت رد شد؛ سفارش در انتظار پرداخت مجازی مشتری باقی می‌ماند."
            : "رد پرداخت ناموفق بود.";
        return RedirectToAction(nameof(Index), new { tab = "finance" });
    }

    // -------------------- Process actions --------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveCompletion(int id, string? note, CancellationToken cancellationToken)
    {
        var adminId = GetAdminId();
        var ok = await _completions.ApproveAsync(id, note, adminId, cancellationToken);
        TempData[ok ? "Success" : "Error"] = ok
            ? "اتمام کار تأیید شد؛ سفارش تکمیل گردید و دستمزد کارشناس (۹۰٪) به همراه کمیسیون سایت (۱۰٪) تسویه شد."
            : "تأیید اتمام کار ناموفق بود (رکورد یافت نشد یا قبلاً بررسی شده است).";
        return RedirectToAction(nameof(Index), new { tab = "process" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectCompletion(int id, string note, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            TempData["Error"] = "برای عدم تأیید، ثبت توضیحات دلایل الزامی است تا به طرفین نمایش داده شود.";
            return RedirectToAction(nameof(Index), new { tab = "process" });
        }

        var adminId = GetAdminId();
        var ok = await _completions.RejectAsync(id, note, adminId, cancellationToken);
        TempData[ok ? "Success" : "Error"] = ok
            ? "عدم تأیید ثبت شد؛ سفارش به حالت «در حال انجام» بازگشت و توضیحات برای طرفین نمایش داده می‌شود."
            : "عدم تأیید ناموفق بود.";
        return RedirectToAction(nameof(Index), new { tab = "process" });
    }

    // -------------------- Ticket actions --------------------
    /// <summary>Ticket thread view for the support agent: conversation + reply + status.</summary>
    public async Task<IActionResult> TicketDetails(int id, CancellationToken cancellationToken)
    {
        var ticket = await _tickets.GetByIdAsync(id, cancellationToken);
        if (ticket == null) return NotFound();

        var user = await _identity.GetUserByIdAsync(ticket.UserId, cancellationToken);
        ViewBag.SubmitterName = user?.FullName ?? "—";
        ViewBag.SubmitterEmail = user?.Email ?? "—";

        return View(ticket);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TicketReply(int id, string body, List<Microsoft.AspNetCore.Http.IFormFile>? files, CancellationToken cancellationToken)
    {
        var adminId = GetAdminId();
        if (string.IsNullOrWhiteSpace(body))
        {
            TempData["Error"] = "متن پاسخ خالی است.";
            return RedirectToAction(nameof(TicketDetails), new { id });
        }

        List<string>? urls = null; List<string?>? thumbs = null; List<MediaType>? types = null;
        if (files != null && files.Any(f => f.Length > 0))
        {
            urls = new List<string>(); thumbs = new List<string?>(); types = new List<MediaType>();
            var fileSvc = HttpContext.RequestServices.GetRequiredService<IFileService>();
            foreach (var file in files.Where(f => f.Length > 0).Take(5))
            {
                var mediaType = file.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
                    ? MediaType.Video : MediaType.Image;
                var media = await fileSvc.SaveMediaAsync(
                    file.OpenReadStream(), file.FileName, file.ContentType, file.Length,
                    mediaType, MediaEntityType.SupportTicketAttachment, adminId, cancellationToken);
                urls.Add(media.OriginalUrl);
                thumbs.Add(media.ThumbnailUrl);
                types.Add(mediaType);
            }
        }

        var ok = await _tickets.ReplyAsync(id, body, adminId, isFromAdmin: true, urls, thumbs, types, cancellationToken);
        TempData[ok ? "Success" : "Error"] = ok
            ? "پاسخ شما برای کاربر ارسال شد."
            : "ارسال پاسخ ناموفق بود (تیکت بسته است).";
        return RedirectToAction(nameof(TicketDetails), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TicketStatus(int id, SupportTicketStatus status, CancellationToken cancellationToken)
    {
        var adminId = GetAdminId();
        var ok = await _tickets.UpdateStatusAsync(id, status, adminId, cancellationToken);
        TempData[ok ? "Success" : "Error"] = ok
            ? $"وضعیت تیکت به «{status.ToDisplay()}» تغییر کرد."
            : "تغییر وضعیت ناموفق بود.";
        return RedirectToAction(nameof(TicketDetails), new { id });
    }

    private Guid GetAdminId()
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(id, out var guid) ? guid : Guid.Empty;
    }
}
