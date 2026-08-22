using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using HomeServices.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Mvc.Controllers;

/// <summary>
/// User-facing help desk: submitting tickets (with document uploads), tracking
/// their status and conversing with the support team through a message thread.
/// </summary>
[Authorize]
public class SupportController : Controller
{
    private readonly ISupportTicketService _tickets;
    private readonly IOrderService _orders;
    private readonly IFileService _files;
    private readonly ILogger<SupportController> _logger;

    public SupportController(
        ISupportTicketService tickets,
        IOrderService orders,
        IFileService files,
        ILogger<SupportController> logger)
    {
        _tickets = tickets;
        _orders = orders;
        _files = files;
        _logger = logger;
    }

    // -------------------- My tickets --------------------
    public async Task<IActionResult> MyTickets(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var tickets = await _tickets.GetByUserAsync(userId.Value, cancellationToken);
        return View(tickets);
    }

    // -------------------- Create ticket --------------------
    [HttpGet]
    public async Task<IActionResult> Create(int? orderId = null, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        // سفارش‌های کاربر برای درج اختیاری در تیکت
        var orders = await _orders.GetByCustomerAsync(userId.Value, cancellationToken);
        ViewBag.Orders = orders;

        return View(new CreateTicketViewModel { OrderId = orderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTicketViewModel vm, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var orders = await _orders.GetByCustomerAsync(userId.Value, cancellationToken);
        ViewBag.Orders = orders;

        if (!ModelState.IsValid) return View(vm);

        try
        {
            var dto = new CreateSupportTicketDto
            {
                OrderId = vm.OrderId,
                Subject = vm.Subject,
                Category = vm.Category,
                Priority = vm.Priority,
                Description = vm.Description,
            };

            if (vm.Files != null && vm.Files.Any(f => f.Length > 0))
            {
                dto.FileUrls = new List<string>();
                dto.ThumbnailUrls = new List<string?>();
                dto.MediaTypes = new List<MediaType>();
                dto.Captions = new List<string>();

                foreach (var file in vm.Files.Where(f => f.Length > 0).Take(5))
                {
                    var mediaType = file.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
                        ? MediaType.Video
                        : file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                            ? MediaType.Image
                            : MediaType.Document;

                    var media = await _files.SaveMediaAsync(
                        file.OpenReadStream(), file.FileName, file.ContentType, file.Length,
                        mediaType, MediaEntityType.SupportTicketAttachment, userId, cancellationToken);

                    dto.FileUrls.Add(media.OriginalUrl);
                    dto.ThumbnailUrls.Add(media.ThumbnailUrl);
                    dto.MediaTypes.Add(mediaType);
                    dto.Captions.Add(file.FileName);
                }
            }

            var ticket = await _tickets.CreateAsync(dto, userId.Value, cancellationToken);
            TempData["Success"] = $"تیکت شما با شماره {ticket.TicketNumber} ثبت شد و کارشناس پشتیبانی به‌زودی بررسی می‌کند.";
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create support ticket for user {UserId}.", userId);
            ModelState.AddModelError(string.Empty, "ثبت تیکت ناموفق بود. دوباره تلاش کنید.");
            return View(vm);
        }
    }

    // -------------------- Ticket details + reply --------------------
    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var ticket = await _tickets.GetByIdAsync(id, cancellationToken);
        if (ticket == null || (ticket.UserId != userId && !User.IsInRole("Admin"))) return Forbid();

        return View(ticket);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(ReplyTicketViewModel vm, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var ticket = await _tickets.GetByIdAsync(vm.TicketId, cancellationToken);
        if (ticket == null || ticket.UserId != userId) return Forbid();

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "متن پیام خالی است.";
            return RedirectToAction(nameof(Details), new { id = vm.TicketId });
        }

        try
        {
            List<string>? urls = null; List<string?>? thumbs = null; List<MediaType>? types = null;

            if (vm.Files != null && vm.Files.Any(f => f.Length > 0))
            {
                urls = new List<string>(); thumbs = new List<string?>(); types = new List<MediaType>();
                foreach (var file in vm.Files.Where(f => f.Length > 0).Take(5))
                {
                    var mediaType = file.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
                        ? MediaType.Video : MediaType.Image;
                    var media = await _files.SaveMediaAsync(
                        file.OpenReadStream(), file.FileName, file.ContentType, file.Length,
                        mediaType, MediaEntityType.SupportTicketAttachment, userId, cancellationToken);
                    urls.Add(media.OriginalUrl);
                    thumbs.Add(media.ThumbnailUrl);
                    types.Add(mediaType);
                }
            }

            var ok = await _tickets.ReplyAsync(vm.TicketId, vm.Body, userId.Value, isFromAdmin: false,
                urls, thumbs, types, cancellationToken);
            if (!ok)
            {
                TempData["Error"] = "این تیکت بسته شده و امکان پیام‌رسانی ندارد.";
                return RedirectToAction(nameof(Details), new { id = vm.TicketId });
            }

            TempData["Success"] = "پیام شما ارسال شد.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reply to ticket {TicketId}.", vm.TicketId);
            TempData["Error"] = "ارسال پیام ناموفق بود.";
        }

        return RedirectToAction(nameof(Details), new { id = vm.TicketId });
    }

    // -------------------- Close own ticket --------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var ticket = await _tickets.GetByIdAsync(id, cancellationToken);
        if (ticket == null || ticket.UserId != userId) return Forbid();

        await _tickets.UpdateStatusAsync(id, SupportTicketStatus.Closed, userId.Value, cancellationToken);
        TempData["Success"] = "تیکت بسته شد. در صورت نیاز می‌توانید تیکت جدید ثبت کنید.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private Guid? GetUserId()
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(id, out var guid) ? guid : null;
    }
}

// -------------------- View models --------------------
public class CreateTicketViewModel
{
    public int? OrderId { get; set; }

    [Display(Name = "موضوع")]
    [Required(ErrorMessage = "موضوع تیکت الزامی است.")]
    [StringLength(200, ErrorMessage = "موضوع حداکثر ۲۰۰ کاراکتر است.")]
    public string Subject { get; set; } = string.Empty;

    [Display(Name = "دسته‌بندی")]
    public SupportTicketCategory Category { get; set; } = SupportTicketCategory.OrderIssue;

    [Display(Name = "اولویت")]
    public SupportTicketPriority Priority { get; set; } = SupportTicketPriority.Normal;

    [Display(Name = "شرح درخواست")]
    [Required(ErrorMessage = "شرح درخواست الزامی است.")]
    [StringLength(4000, ErrorMessage = "شرح حداکثر ۴۰۰۰ کاراکتر است.")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "مستندات (عکس/ویدیو/فایل)")]
    [MaxFileSize(10 * 1024 * 1024)]
    public List<IFormFile>? Files { get; set; }
}

public class ReplyTicketViewModel
{
    public int TicketId { get; set; }

    [Required(ErrorMessage = "متن پیام الزامی است.")]
    [StringLength(4000)]
    public string Body { get; set; } = string.Empty;

    public List<IFormFile>? Files { get; set; }
}

/// <summary>Rejects uploads larger than the given byte size.</summary>
public class MaxFileSizeAttribute : ValidationAttribute
{
    private readonly long _maxBytes;
    public MaxFileSizeAttribute(long maxBytes) => _maxBytes = maxBytes;

    public override bool IsValid(object? value)
    {
        if (value is not List<IFormFile> files || files.Count == 0) return true;
        return files.All(f => f.Length <= _maxBytes);
    }

    public override string FormatErrorMessage(string name) => "حجم هر فایل حداکثر ۱۰ مگابایت است.";
}
