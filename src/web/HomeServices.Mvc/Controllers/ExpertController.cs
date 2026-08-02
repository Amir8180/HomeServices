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
    private readonly ILogger<ExpertController> _logger;

    public ExpertController(
        IExpertProfileService experts,
        IServiceRequestService requests,
        IProposalService proposals,
        IOrderService orders,
        ICategoryService categories,
        IReviewService reviews,
        IFileService files,
        ILogger<ExpertController> logger)
    {
        _experts = experts;
        _requests = requests;
        _proposals = proposals;
        _orders = orders;
        _categories = categories;
        _reviews = reviews;
        _files = files;
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
        ViewBag.OpenProposalsCount = myProposals.Count(p => p.Status == ProposalStatus.Pending);
        ViewBag.AcceptedProposalsCount = myProposals.Count(p => p.Status == ProposalStatus.Accepted);
        ViewBag.ActiveOrdersCount = myOrders.Count(o => o.Status == OrderStatus.Scheduled || o.Status == OrderStatus.InProgress);
        ViewBag.CompletedOrdersCount = myOrders.Count(o => o.Status == OrderStatus.Completed);
        ViewBag.PendingPayout = myOrders.Where(o => o.Status == OrderStatus.Completed).Sum(o => o.TotalAmount);

        // Recent active jobs for the side panel.
        ViewBag.ActiveOrders = myOrders
            .Where(o => o.Status == OrderStatus.Scheduled || o.Status == OrderStatus.InProgress)
            .Take(5)
            .ToList();

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

    // -------------------- My proposals --------------------
    public async Task<IActionResult> MyProposals(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var proposals = await _proposals.GetByExpertAsync(userId.Value, cancellationToken);
        return View(proposals);
    }

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

    // -------------------- Active jobs (orders) --------------------
    public async Task<IActionResult> MyJobs(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var orders = await _orders.GetByExpertAsync(userId.Value, cancellationToken);
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
        var allowed = status switch
        {
            // Expert confirms the customer's requested date → scheduled.
            OrderStatus.Scheduled when order.Status == OrderStatus.Paid => true,
            OrderStatus.InProgress when order.Status == OrderStatus.Scheduled || order.Status == OrderStatus.Paid => true,
            OrderStatus.Completed when order.Status == OrderStatus.InProgress => true,
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
    public string BusinessName { get; set; } = "";

    [StringLength(2000)] public string? Bio { get; set; }
    public string? LogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    [StringLength(200)] public string? ServiceArea { get; set; }
    [StringLength(100)] public string? City { get; set; }
    [StringLength(200)] public string? BusinessHours { get; set; }
    [Range(1, 10080)] public int? ResponseTimeMinutes { get; set; }
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
