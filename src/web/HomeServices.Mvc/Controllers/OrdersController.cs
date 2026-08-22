using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HomeServices.Application.Common;
using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using HomeServices.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Mvc.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly IOrderService _orders;
    private readonly IServiceRequestService _requests;
    private readonly ISiteSettingService _siteSettings;
    private readonly IPaymentVerificationService _paymentReports;
    private readonly IWorkCompletionService _completions;
    private readonly IReviewService _reviews;
    private readonly IFileService _files;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        IOrderService orders,
        IServiceRequestService requests,
        ISiteSettingService siteSettings,
        IPaymentVerificationService paymentReports,
        IWorkCompletionService completions,
        IReviewService reviews,
        IFileService files,
        ILogger<OrdersController> logger)
    {
        _orders = orders;
        _requests = requests;
        _siteSettings = siteSettings;
        _paymentReports = paymentReports;
        _completions = completions;
        _reviews = reviews;
        _files = files;
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

        // Latest payment report + completion report for the status panels.
        ViewBag.PaymentReport = order.Status is OrderStatus.WaitingPaymentVerification
            ? (await _paymentReports.GetPagedAsync(
                new PaymentVerificationFilterDto { Page = 1, PageSize = 1 }, HttpContext.RequestAborted)).Items
                .FirstOrDefault(r => r.OrderId == order.Id)
            : null;
        ViewBag.CompletionReport = await _completions.GetByOrderIdAsync(order.Id, HttpContext.RequestAborted);
        // Submitted review (if any) — hides the review button after submission.
        ViewBag.ExistingReview = await _reviews.GetByOrderIdAsync(order.Id, HttpContext.RequestAborted);

        return View(order);
    }

    /// <summary>
    /// Card-to-card payment page (no gateway). Shows the payable amount, the bank
    /// card number and holder (from SiteSettings, with hard-coded fallbacks), the
    /// instruction to send the receipt, and a button that opens the support
    /// Telegram private chat (pv) where the customer sends the transfer receipt.
    /// A separate submit form lets the customer register the payment report that
    /// lands in the admin support dashboard for verification.
    /// </summary>
    [Authorize(Policy = "CustomerOnly")]
    public async Task<IActionResult> Pay(int id)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var order = await _orders.GetByIdAsync(id, HttpContext.RequestAborted);
        if (order == null || order.CustomerId != userId) return Forbid();

        if (order.Status != OrderStatus.PendingPayment && order.Status != OrderStatus.WaitingPaymentVerification)
        {
            TempData["Info"] = "این سفارش در انتظار پرداخت نیست.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Resolve card details from SiteSettings; fall back to defaults when missing.
        var settings = await _siteSettings.GetAllAsDictionaryAsync(HttpContext.RequestAborted);
        var (cardNumber, cardHolder, telegramUrl) = CardToCardPaymentInfo.Resolve(settings);

        ViewBag.CardNumber = cardNumber;
        ViewBag.CardHolder = cardHolder;
        ViewBag.TelegramUrl = telegramUrl;
        ViewBag.ReceiptInstruction = CardToCardPaymentInfo.ReceiptInstruction;

        // Pending report (if the customer already submitted one).
        var reports = await _paymentReports.GetPagedAsync(
            new PaymentVerificationFilterDto { Page = 1, PageSize = 5 }, HttpContext.RequestAborted);
        ViewBag.PendingReport = reports.Items.FirstOrDefault(r =>
            r.OrderId == order.Id && r.Status == PaymentVerificationStatus.PendingReview);
        ViewBag.CustomerName = User.FindFirst(ClaimTypes.Name)?.Value ?? "";

        return View(order);
    }

    // -------------------- Submit card-to-card payment report --------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<IActionResult> SubmitPayment(SubmitPaymentViewModel vm)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var order = await _orders.GetByIdAsync(vm.OrderId, HttpContext.RequestAborted);
        if (order == null || order.CustomerId != userId) return Forbid();

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "اطلاعات فرم پرداخت کامل نیست.";
            return RedirectToAction(nameof(Pay), new { id = vm.OrderId });
        }

        try
        {
            var dto = new CreatePaymentVerificationReportDto
            {
                OrderId = vm.OrderId,
                Amount = vm.Amount,
                SenderFullName = vm.SenderFullName,
                BankRefNumber = vm.BankRefNumber,
                CustomerNote = vm.CustomerNote,
            };
            await _paymentReports.CreateAsync(dto, userId.Value, HttpContext.RequestAborted);
            TempData["Success"] = "گزارش پرداخت شما ثبت شد و پس از بررسی پشتیبانی، سفارش شما به حالت «پرداخت‌شده» تغییر می‌کند.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Info"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit payment report for order {OrderId}.", vm.OrderId);
            TempData["Error"] = "ثبت گزارش پرداخت ناموفق بود. دوباره تلاش کنید.";
        }

        return RedirectToAction(nameof(Details), new { id = vm.OrderId });
    }

    // -------------------- Customer confirms work completion --------------------
    [HttpGet]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<IActionResult> ConfirmCompletion(int orderId)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var order = await _orders.GetByIdAsync(orderId, HttpContext.RequestAborted);
        if (order == null || order.CustomerId != userId) return Forbid();

        ViewBag.Order = order;
        return View(new ConfirmCompletionViewModel { OrderId = orderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<IActionResult> ConfirmCompletion(ConfirmCompletionViewModel vm)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var order = await _orders.GetByIdAsync(vm.OrderId, HttpContext.RequestAborted);
        if (order == null || order.CustomerId != userId) return Forbid();

        if (!ModelState.IsValid)
        {
            ViewBag.Order = order;
            return View(vm);
        }

        try
        {
            var attachments = await SaveAttachmentsAsync(vm.Files, userId.Value, AttachmentUploader.Customer);

            var dto = new CreateWorkCompletionDeclarationDto
            {
                OrderId = vm.OrderId,
                Confirmed = vm.Confirmed,
                Note = vm.Note,
                FileUrls = attachments?.Select(a => a.url).ToList(),
                ThumbnailUrls = attachments?.Select(a => a.thumbnailUrl).ToList(),
                MediaTypes = attachments?.Select(a => a.mediaType).ToList(),
            };
            await _completions.DeclareCompletionAsync(dto, userId.Value, AttachmentUploader.Customer, HttpContext.RequestAborted);
            TempData["Success"] = vm.Confirmed
                ? "تأیید اتمام کار شما ثبت شد و برای بررسی پشتیبانی ارسال گردید."
                : "موضع شما (عدم تأیید اتمام کار) با توضیحات ثبت و برای پشتیبانی ارسال شد.";
            return RedirectToAction(nameof(Details), new { id = vm.OrderId });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Info"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to confirm completion for order {OrderId}.", vm.OrderId);
            TempData["Error"] = "ثبت تأیید اتمام کار ناموفق بود.";
        }

        return RedirectToAction(nameof(Details), new { id = vm.OrderId });
    }

    // -------------------- Shared helpers --------------------
    private async Task<List<(string url, string? thumbnailUrl, MediaType mediaType)>?> SaveAttachmentsAsync(
        List<IFormFile>? files, Guid userId, AttachmentUploader uploader)
    {
        if (files == null || files.Count == 0) return null;

        var result = new List<(string, string?, MediaType)>();
        foreach (var file in files.Where(f => f.Length > 0).Take(5))
        {
            var mediaType = file.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
                ? MediaType.Video
                : MediaType.Image;

            var media = await _files.SaveMediaAsync(
                file.OpenReadStream(), file.FileName, file.ContentType, file.Length,
                mediaType, MediaEntityType.CompletionAttachment, userId, HttpContext.RequestAborted);

            result.Add((media.OriginalUrl, media.ThumbnailUrl, mediaType));
        }
        return result;
    }

    private Guid? GetUserId()
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(id, out var guid) ? guid : null;
    }
}

// -------------------- View models --------------------

public class SubmitPaymentViewModel
{
    public int OrderId { get; set; }

    [Display(Name = "مبلغ پرداخت‌شده (تومان)")]
    [Required(ErrorMessage = "مبلغ پرداخت‌شده را وارد کنید.")]
    [Range(1000, 10_000_000_000, ErrorMessage = "مبلغ معتبر نیست.")]
    public decimal Amount { get; set; }

    [Display(Name = "نام و نام خانوادگی واریزکننده")]
    [Required(ErrorMessage = "نام واریزکننده الزامی است.")]
    [StringLength(200)]
    public string SenderFullName { get; set; } = string.Empty;

    [Display(Name = "شماره پیگیری تراکنش")]
    [StringLength(100)]
    public string? BankRefNumber { get; set; }

    [Display(Name = "توضیحات")]
    [StringLength(2000)]
    public string? CustomerNote { get; set; }
}

public class ConfirmCompletionViewModel
{
    public int OrderId { get; set; }

    [Display(Name = "توضیحات / دلایل")]
    [StringLength(4000)]
    public string? Note { get; set; }

    /// <summary>true = تأیید اتمام کار، false = اعلام عدم رضایت/عدم اتمام.</summary>
    public bool Confirmed { get; set; } = true;

    [Display(Name = "مستندات (عکس/ویدیو)")]
    public List<IFormFile>? Files { get; set; }
}
