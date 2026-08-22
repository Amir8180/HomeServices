using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using HomeServices.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Mvc.Controllers;

/// <summary>
/// Expert (professional) area: dashboard, browsing open requests, submitting and
/// managing proposals, handling active jobs, managing portfolio and profile.
/// All actions require the Expert role.
/// </summary>
[Authorize(Policy = "ExpertOnly")]
public class ExpertController : Controller
{
    private readonly IExpertProfileService _experts;
    private readonly IServiceRequestService _requests;
    private readonly IProposalService _proposals;
    private readonly IOrderService _orders;
    private readonly ICategoryService _categories;
    private readonly IReviewService _reviews;
    private readonly IFileService _files;
    private readonly IWorkCompletionService _completions;
    private readonly IExpertPayoutService _payouts;
    private readonly ILogger<ExpertController> _logger;

    public ExpertController(
        IExpertProfileService experts,
        IServiceRequestService requests,
        IProposalService proposals,
        IOrderService orders,
        ICategoryService categories,
        IReviewService reviews,
        IFileService files,
        IWorkCompletionService completions,
        IExpertPayoutService payouts,
        ILogger<ExpertController> logger)
    {
        _experts = experts;
        _requests = requests;
        _proposals = proposals;
        _orders = orders;
        _categories = categories;
        _reviews = reviews;
        _files = files;
        _completions = completions;
        _payouts = payouts;
        _logger = logger;
    }

    // -------------------- Dashboard --------------------
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var profile = await _experts.GetByUserIdAsync(userId.Value, cancellationToken);
        if (profile == null)
        {
            // Onboarding guard: a profile should exist (auto-created on register),
            // but if missing we send the expert to the profile editor.
            return RedirectToAction(nameof(Profile));
        }

        var myProposals = await _proposals.GetByExpertAsync(userId.Value, cancellationToken);
        var myOrders = await _orders.GetByExpertAsync(userId.Value, cancellationToken);

        ViewBag.Profile = profile;

        var pendingProposals   = myProposals.Count(p => p.Status == ProposalStatus.Pending);
        var acceptedProposals  = myProposals.Count(p => p.Status == ProposalStatus.Accepted);
        var rejectedProposals  = myProposals.Count(p => p.Status == ProposalStatus.Rejected);
        ViewBag.OpenProposalsCount = pendingProposals;
        ViewBag.AcceptedProposalsCount = acceptedProposals;

        // سفارش فعال = هر سفارشی که هنوز تمام/لغو نشده — از ثبت اولیه (در انتظار پرداخت مشتری)
        // تا بررسی اتمام توسط پشتیبانی. سفارش‌های جدید بلافاصله اینجا ظاهر می‌شوند.
        var activeOrdersList = myOrders
            .Where(o => o.Status is OrderStatus.PendingPayment
                               or OrderStatus.WaitingPaymentVerification
                               or OrderStatus.Paid
                               or OrderStatus.Scheduled
                               or OrderStatus.InProgress
                               or OrderStatus.CompletionReview)
            .OrderByDescending(o => o.CreatedAt)
            .ToList();
        var completedOrdersCount = myOrders.Count(o => o.Status == OrderStatus.Completed);
        ViewBag.ActiveOrdersCount = activeOrdersList.Count;
        ViewBag.CompletedOrdersCount = completedOrdersCount;

        // متریک‌های واقعی از دیتابیس:
        var decidedProposals = acceptedProposals + rejectedProposals;
        ViewBag.WinRate = decidedProposals > 0 ? (int)Math.Round(acceptedProposals * 100.0 / decidedProposals) : 0;
        ViewBag.TotalOrders = myOrders.Count;
        ViewBag.CompletionRate = myOrders.Count > 0 ? (int)Math.Round(completedOrdersCount * 100.0 / myOrders.Count) : 0;

        // درآمد واقعی از تسویه‌های ثبت‌شده (خالص پس از کسر کمیسیون)
        var incomeSummary = await _payouts.GetExpertIncomeSummaryAsync(userId.Value, "daily", cancellationToken);
        ViewBag.TotalEarnings = incomeSummary.TotalIncome;
        ViewBag.TodayIncome = incomeSummary.TodayIncome;
        ViewBag.MonthlyTrend = await _payouts.GetExpertMonthlyTrendAsync(userId.Value, 6, cancellationToken);

        // سفارش‌های فعال برای پنل داشبورد
        ViewBag.ActiveOrders = activeOrdersList.Take(5).ToList();
        ViewBag.RecentProposals = myProposals.OrderByDescending(p => p.CreatedAt).Take(5).ToList();

        return View();
    }

    // -------------------- Open requests marketplace --------------------
    public async Task<IActionResult> OpenRequests(
        int? categoryId = null, string? search = null, string? city = null,
        UrgencyLevel? urgency = null, int page = 1, CancellationToken cancellationToken = default)
    {
        var filter = new ServiceRequestFilterDto
        {
            CategoryId = categoryId,
            SearchTerm = search,
            City = city,
            Urgency = urgency,
            Page = page,
            PageSize = 12,
            Status = RequestStatus.Open, // experts only see requests still accepting proposals
        };

        var result = await _requests.GetPagedAsync(filter, cancellationToken);
        ViewBag.Categories = await _categories.GetAllAsync(true, cancellationToken);

        // Mark requests the expert has already quoted so the UI can reflect that.
        var userId = GetUserId();
        if (userId != null)
        {
            var myProposalRequestIds = (await _proposals.GetByExpertAsync(userId.Value, cancellationToken))
                .Where(p => p.Status != ProposalStatus.Withdrawn)
                .Select(p => p.RequestId)
                .ToHashSet();
            ViewBag.MyProposalRequestIds = myProposalRequestIds;
        }

        ViewBag.CurrentFilter = filter;
        return View(result);
    }

    // -------------------- Submit a proposal --------------------
    [HttpGet]
    public async Task<IActionResult> CreateProposal(int requestId, CancellationToken cancellationToken)
    {
        var request = await _requests.GetByIdAsync(requestId, cancellationToken);
        if (request == null) return NotFound();
        if (request.Status != RequestStatus.Open)
        {
            TempData["Error"] = "این درخواست دیگر پیشنهاد نمی‌پذیرد.";
            return RedirectToAction(nameof(OpenRequests));
        }

        var userId = GetUserId();
        if (userId != null)
        {
            // Block duplicate proposals on the same request by the same expert.
            var existing = (await _proposals.GetByExpertAsync(userId.Value, cancellationToken))
                .FirstOrDefault(p => p.RequestId == requestId && p.Status != ProposalStatus.Withdrawn);
            if (existing != null)
            {
                TempData["Info"] = "شما قبلاً برای این درخواست پیشنهاد ثبت کرده‌اید.";
                return RedirectToAction(nameof(MyProposals));
            }
        }

        ViewBag.Request = request;
        return View(new CreateProposalViewModel { RequestId = requestId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProposal(CreateProposalViewModel model, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var request = await _requests.GetByIdAsync(model.RequestId, cancellationToken);
        if (request == null) return NotFound();

        if (!ModelState.IsValid)
        {
            ViewBag.Request = request;
            return View(model);
        }

        if (request.Status != RequestStatus.Open)
        {
            TempData["Error"] = "این درخواست دیگر پیشنهاد نمی‌پذیرد.";
            return RedirectToAction(nameof(OpenRequests));
        }

        try
        {
            var dto = new CreateProposalDto
            {
                RequestId = model.RequestId,
                Price = model.Price,
                EstimatedDurationHours = model.EstimatedDurationHours,
                Message = model.Message,
                AvailableStartDate = model.AvailableStartDate,
            };
            await _proposals.CreateAsync(dto, userId.Value, cancellationToken);
            TempData["Success"] = "پیشنهاد شما با موفقیت ارسال شد.";
            return RedirectToAction(nameof(MyProposals));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create proposal for request {RequestId}.", model.RequestId);
            ModelState.AddModelError(string.Empty, "ارسال پیشنهاد ناموفق بود. دوباره تلاش کنید.");
            ViewBag.Request = request;
            return View(model);
        }
    }

    // -------------------- My proposals (unified into سفارشات من) --------------------
    // Legacy route: redirects to the unified orders page (MyJobs) so old links and
    // post-action redirects keep working with a single hub.
    public IActionResult MyProposals()
        => RedirectToAction(nameof(MyJobs), new { status = "Pending" });

    // -------------------- Withdraw a pending proposal --------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WithdrawProposal(int id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var proposal = await _proposals.GetByIdAsync(id, cancellationToken);
        if (proposal == null || proposal.ExpertId != userId) return Forbid();
        if (proposal.Status != ProposalStatus.Pending)
        {
            TempData["Error"] = "فقط پیشنهادهای در انتظار قابل برگشت هستند.";
            return RedirectToAction(nameof(MyProposals));
        }

        var ok = await _proposals.UpdateStatusAsync(id, ProposalStatus.Withdrawn, cancellationToken);
        TempData[ok ? "Success" : "Error"] = ok ? "پیشنهاد برگشت داده شد." : "عملیات ناموفق بود.";
        return RedirectToAction(nameof(MyProposals));
    }

    // -------------------- Unified orders page (سفارشات من) --------------------
    // Displays orders (active / completed) and proposals (accepted / pending) as
    // filterable sections so the expert has a single hub for all their work.
    public async Task<IActionResult> MyJobs(string? status = null, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var orders = await _orders.GetByExpertAsync(userId.Value, cancellationToken);
        var proposals = await _proposals.GetByExpertAsync(userId.Value, cancellationToken);

        ViewBag.StatusFilter = status;
        ViewBag.Proposals = proposals;
        return View(orders);
    }

    // -------------------- Advance an order's status --------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(int id, OrderStatus status, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var order = await _orders.GetByIdAsync(id, cancellationToken);
        if (order == null || order.ExpertId != userId) return Forbid();

        // Experts may only progress their jobs forward along the allowed path.
        // NOTE: completion is intentionally NOT allowed here — the expert must go
        // through MarkComplete so the dual-confirmation/support-review flow runs.
        var allowed = status switch
        {
            // Expert confirms the customer's requested date → scheduled.
            OrderStatus.Scheduled when order.Status == OrderStatus.Paid => true,
            OrderStatus.InProgress when order.Status == OrderStatus.Scheduled || order.Status == OrderStatus.Paid => true,
            _ => false,
        };
        if (!allowed)
        {
            TempData["Error"] = "تغییر وضعیت مجاز نیست.";
            return RedirectToAction("Details", "Orders", new { id });
        }

        await _orders.UpdateStatusAsync(id, status, cancellationToken);
        TempData["Success"] = "وضعیت سفارش به‌روزرسانی شد.";
        return RedirectToAction("Details", "Orders", new { id });
    }

    // -------------------- Declare work completion (with note + media evidence) --------------------
    [HttpGet]
    public async Task<IActionResult> MarkComplete(int id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var order = await _orders.GetByIdAsync(id, cancellationToken);
        if (order == null || order.ExpertId != userId) return Forbid();

        if (order.Status != OrderStatus.InProgress && order.Status != OrderStatus.Scheduled && order.Status != OrderStatus.Paid)
        {
            TempData["Info"] = "اعلام اتمام کار برای این وضعیت سفارش ممکن نیست.";
            return RedirectToAction("Details", "Orders", new { id });
        }

        ViewBag.Order = order;
        return View(new MarkCompleteViewModel { OrderId = id, Confirmed = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkComplete(MarkCompleteViewModel model, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var order = await _orders.GetByIdAsync(model.OrderId, cancellationToken);
        if (order == null || order.ExpertId != userId) return Forbid();

        if (!ModelState.IsValid)
        {
            ViewBag.Order = order;
            return View(model);
        }

        try
        {
            List<string>? urls = null, thumbs = null, captions = null;
            List<MediaType>? types = null;

            if (model.Files != null && model.Files.Any(f => f.Length > 0))
            {
                urls = new List<string>(); thumbs = new List<string?>(); types = new List<MediaType>(); captions = new List<string>();
                foreach (var file in model.Files.Where(f => f.Length > 0).Take(5))
                {
                    var mediaType = file.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
                        ? MediaType.Video : MediaType.Image;
                    var media = await _files.SaveMediaAsync(
                        file.OpenReadStream(), file.FileName, file.ContentType, file.Length,
                        mediaType, MediaEntityType.CompletionAttachment, userId, cancellationToken);
                    urls.Add(media.OriginalUrl);
                    thumbs.Add(media.ThumbnailUrl);
                    types.Add(mediaType);
                    captions.Add(file.FileName);
                }
            }

            var dto = new CreateWorkCompletionDeclarationDto
            {
                OrderId = model.OrderId,
                Confirmed = model.Confirmed,
                Note = model.Note,
                FileUrls = urls,
                ThumbnailUrls = thumbs,
                MediaTypes = types,
                Captions = captions,
            };
            await _completions.DeclareCompletionAsync(dto, userId.Value, AttachmentUploader.Expert, cancellationToken);
            TempData["Success"] = "اعلام اتمام کار ثبت شد و برای بررسی و تأیید نهایی به پشتیبانی ارسال گردید.";
            return RedirectToAction("Details", "Orders", new { id = model.OrderId });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Info"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to declare completion for order {OrderId}.", model.OrderId);
            ModelState.AddModelError(string.Empty, "ثبت اعلام اتمام کار ناموفق بود. دوباره تلاش کنید.");
            ViewBag.Order = order;
            return View(model);
        }

        return RedirectToAction("Details", "Orders", new { id = model.OrderId });
    }

    // -------------------- Financial management --------------------
    public async Task<IActionResult> Finance(string period = "daily", CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var summary = await _payouts.GetExpertIncomeSummaryAsync(userId.Value, period, cancellationToken);
        ViewBag.Period = period;
        return View(summary);
    }

    public async Task<IActionResult> Payouts(int page = 1, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var result = await _payouts.GetPagedAsync(
            new ExpertPayoutFilterDto { ExpertId = userId, Page = page, PageSize = 20 }, cancellationToken);
        return View(result);
    }

    public async Task<IActionResult> PayoutInvoice(int id, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var payout = await _payouts.GetByIdAsync(id, cancellationToken);
        if (payout == null || (payout.ExpertId != userId && !User.IsInRole("Admin"))) return Forbid();

        var profile = await _experts.GetByUserIdAsync(userId.Value, cancellationToken);
        ViewBag.ExpertName = profile?.BusinessName ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        return View(payout);
    }

    // -------------------- Reviews received --------------------
    public async Task<IActionResult> MyReviews(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var reviews = await _reviews.GetByExpertAsync(userId.Value, cancellationToken);
        return View(reviews);
    }

    // -------------------- Profile (own business profile) --------------------
    [HttpGet]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var profile = await _experts.GetByUserIdAsync(userId.Value, cancellationToken);
        var categories = await _categories.GetAllAsync(true, cancellationToken);

        var model = new ExpertProfileViewModel
        {
            Id = profile?.Id ?? 0,
            BusinessName = profile?.BusinessName ?? "",
            Bio = profile?.Bio,
            LogoUrl = profile?.LogoUrl,
            CoverImageUrl = profile?.CoverImageUrl,
            ServiceArea = profile?.ServiceArea,
            City = profile?.City,
            BusinessHours = profile?.BusinessHours,
            ResponseTimeMinutes = profile?.ResponseTimeMinutes,
            IsActive = profile?.IsActive ?? true,
            CategoryIds = profile?.CategoryIds.ToList() ?? new List<int>(),
            Categories = categories,
        };

        // Portfolio gallery (read from the already-loaded profile DTO).
        ViewBag.Portfolio = profile?.PortfolioImages ?? Array.Empty<ExpertPortfolioImageDto>();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ExpertProfileViewModel model, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        model.Categories = await _categories.GetAllAsync(true, cancellationToken);

        if (!ModelState.IsValid) return View(model);

        try
        {
            var dto = new UpdateExpertProfileDto
            {
                BusinessName = model.BusinessName,
                Bio = model.Bio,
                LogoUrl = model.LogoUrl,
                CoverImageUrl = model.CoverImageUrl,
                ServiceArea = model.ServiceArea,
                City = model.City,
                BusinessHours = model.BusinessHours,
                ResponseTimeMinutes = model.ResponseTimeMinutes,
                IsActive = model.IsActive,
                CategoryIds = model.CategoryIds,
            };

            if (model.Id == 0)
            {
                // Safety net: create the profile if it was never provisioned.
                await _experts.CreateAsync(new CreateExpertProfileDto
                {
                    UserId = userId.Value,
                    BusinessName = model.BusinessName,
                    Bio = model.Bio,
                    City = model.City,
                    ServiceArea = model.ServiceArea,
                    BusinessHours = model.BusinessHours,
                    CategoryIds = model.CategoryIds,
                }, cancellationToken);
            }
            else
            {
                await _experts.UpdateAsync(model.Id, dto, cancellationToken);
            }

            TempData["Success"] = "پروفایل کارشناسی به‌روزرسانی شد.";
            return RedirectToAction(nameof(Profile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update expert profile for user {UserId}.", userId);
            ModelState.AddModelError(string.Empty, "به‌روزرسانی پروفایل ناموفق بود.");
            return View(model);
        }
    }

    // -------------------- Portfolio management --------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPortfolioImage(ExpertPortfolioImageViewModel model, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        if (model.Image == null || model.Image.Length == 0)
        {
            TempData["Error"] = "لطفاً یک تصویر انتخاب کنید.";
            return RedirectToAction(nameof(Profile));
        }

        try
        {
            await using var stream = model.Image.OpenReadStream();
            var media = await _files.SaveImageAsync(
                stream, model.Image.FileName, model.Image.ContentType, model.Image.Length,
                MediaEntityType.ExpertPortfolio, userId.Value, cancellationToken);

            await _experts.AddPortfolioImageAsync(
                userId.Value, media.OriginalUrl, media.ThumbnailUrl, model.Title, cancellationToken);

            TempData["Success"] = "تصویر نمونه‌کار اضافه شد.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add portfolio image for user {UserId}.", userId);
            TempData["Error"] = "بارگذاری تصویر ناموفق بود.";
        }
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePortfolioImage(int id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var ok = await _experts.DeletePortfolioImageAsync(id, userId.Value, cancellationToken);
        TempData[ok ? "Success" : "Error"] = ok ? "تصویر حذف شد." : "حذف ناموفق بود.";
        return RedirectToAction(nameof(Profile));
    }

    // -------------------- helper --------------------
    private Guid? GetUserId()
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(id, out var guid) ? guid : null;
    }
}

// -------------------- View Models --------------------
public class CreateProposalViewModel
{
    public int RequestId { get; set; }

    [Required(ErrorMessage = "مبلغ پیشنهاد الزامی است.")]
    [Range(10000, 5000000000, ErrorMessage = "مبلغ باید بین ۱۰٬۰۰۰ و ۵ میلیارد تومان باشد.")]
    public decimal Price { get; set; }

    [Range(1, 1000, ErrorMessage = "مدت زمان معتبر نیست.")]
    public int? EstimatedDurationHours { get; set; }

    [StringLength(2000, ErrorMessage = "پیام نمی‌تواند بیش از ۲۰۰۰ کاراکتر باشد.")]
    public string? Message { get; set; }

    [DataType(DataType.Date)]
    public DateTime? AvailableStartDate { get; set; }
}

public class ExpertProfileViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام کسب‌وکار الزامی است.")]
    [StringLength(150)]
    [Display(Name = "نام کسب‌وکار")]
    public string BusinessName { get; set; } = "";

    [StringLength(2000)]
    [Display(Name = "درباره من")]
    public string? Bio { get; set; }

    public string? LogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }

    [StringLength(200)]
    [Display(Name = "محدوده خدمات‌دهی")]
    public string? ServiceArea { get; set; }

    [StringLength(100)]
    [Display(Name = "شهر")]
    public string? City { get; set; }

    [StringLength(200)]
    [Display(Name = "ساعات کاری")]
    public string? BusinessHours { get; set; }

    [Range(1, 10080)]
    [Display(Name = "زمان پاسخ‌گویی")]
    public int? ResponseTimeMinutes { get; set; }

    public bool IsActive { get; set; } = true;
    public List<int> CategoryIds { get; set; } = new();

    // Display only
    public IReadOnlyList<CategoryDto> Categories { get; set; } = Array.Empty<CategoryDto>();
}

public class ExpertPortfolioImageViewModel
{
    [Required(ErrorMessage = "تصویر الزامی است.")]
    public IFormFile Image { get; set; } = null!;
    [StringLength(150)] public string? Title { get; set; }
}

public class MarkCompleteViewModel
{
    public int OrderId { get; set; }

    /// <summary>true = اعلام اتمام کار، false = اعلام مشکل/عدم اتمام.</summary>
    public bool Confirmed { get; set; } = true;

    [Display(Name = "توضیحات / دلایل")]
    [StringLength(4000)]
    public string? Note { get; set; }

    [Display(Name = "مستندات (عکس/ویدیو)")]
    public List<IFormFile>? Files { get; set; }
}
