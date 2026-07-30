using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using HomeServices.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Mvc.Controllers;

[Authorize]
public class ServiceRequestsController : Controller
{
    private readonly IServiceRequestService _requests;
    private readonly IServiceService _services;
    private readonly ICategoryService _categories;
    private readonly IFileService _files;
    private readonly ILogger<ServiceRequestsController> _logger;

    public ServiceRequestsController(
        IServiceRequestService requests,
        IServiceService services,
        ICategoryService categories,
        IFileService files,
        ILogger<ServiceRequestsController> logger)
    {
        _requests = requests;
        _services = services;
        _categories = categories;
        _files = files;
        _logger = logger;
    }

    // -------------------- List (open requests marketplace + my requests) --------------------
    [AllowAnonymous]
    public async Task<IActionResult> Index(
        int? categoryId = null, string? search = null, string? city = null,
        UrgencyLevel? urgency = null, int page = 1)
    {
        var filter = new ServiceRequestFilterDto
        {
            CategoryId = categoryId,
            SearchTerm = search,
            City = city,
            Urgency = urgency,
            Page = page,
            PageSize = 12,
            Status = RequestStatus.Open, // public board shows open requests only
        };

        var result = await _requests.GetPagedAsync(filter, HttpContext.RequestAborted);
        ViewBag.Categories = await _categories.GetAllAsync(true, HttpContext.RequestAborted);
        ViewBag.CurrentFilter = filter;
        return View(result);
    }

    // -------------------- Details --------------------
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var request = await _requests.GetByIdAsync(id, HttpContext.RequestAborted);
        if (request == null) return NotFound();
        return View(request);
    }

    // -------------------- Create --------------------
    [HttpGet]
    public async Task<IActionResult> Create(int? serviceId = null, int? categoryId = null)
    {
        var vm = new CreateServiceRequestViewModel
        {
            Categories = await _categories.GetAllAsync(true, HttpContext.RequestAborted),
        };

        if (serviceId.HasValue)
        {
            var svc = await _services.GetByIdAsync(serviceId.Value, HttpContext.RequestAborted);
            if (svc != null)
            {
                vm.CategoryId = svc.CategoryId;
                vm.ServiceId = svc.Id;
                vm.Title = svc.Title;
            }
        }
        else if (categoryId.HasValue)
        {
            vm.CategoryId = categoryId.Value;
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateServiceRequestViewModel vm)
    {
        vm.Categories = await _categories.GetAllAsync(true, HttpContext.RequestAborted);

        if (!ModelState.IsValid) return View(vm);

        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var dto = new CreateServiceRequestDto
        {
            CategoryId = vm.CategoryId,
            ServiceId = vm.ServiceId,
            Title = vm.Title,
            Description = vm.Description,
            Address = vm.Address,
            City = vm.City,
            ZipCode = vm.ZipCode,
            Latitude = vm.Latitude,
            Longitude = vm.Longitude,
            Urgency = vm.Urgency,
            PreferredDate = vm.PreferredDate,
            BudgetMin = vm.BudgetMin,
            BudgetMax = vm.BudgetMax,
            IsHomeOwner = vm.IsHomeOwner,
        };

        try
        {
            var created = await _requests.CreateAsync(dto, userId.Value, HttpContext.RequestAborted);

            // Attach uploaded images, if any.
            if (vm.Images != null && vm.Images.Any())
            {
                await AttachImagesAsync(created.Id, vm.Images, userId.Value);
            }

            TempData["Success"] = "درخواست شما با موفقیت ثبت شد.";
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create service request.");
            ModelState.AddModelError(string.Empty, "ثبت درخواست ناموفق بود. لطفاً دوباره تلاش کنید.");
            return View(vm);
        }
    }

    private async Task AttachImagesAsync(int requestId, IEnumerable<IFormFile> images, Guid userId)
    {
        foreach (var img in images.Where(f => f.Length > 0))
        {
            await using var stream = img.OpenReadStream();
            var media = await _files.SaveImageAsync(
                stream, img.FileName, img.ContentType, img.Length,
                MediaEntityType.Request, userId, HttpContext.RequestAborted);

            // Persist the association so the request gallery shows real images.
            await _requests.AddImageAsync(requestId, media.OriginalUrl, media.ThumbnailUrl, HttpContext.RequestAborted);
        }
    }

    // -------------------- Cancel --------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var request = await _requests.GetByIdAsync(id, HttpContext.RequestAborted);
        if (request == null || request.CustomerId != userId)
            return Forbid();

        if (request.Status == RequestStatus.Open || request.Status == RequestStatus.Quoted)
        {
            await _requests.UpdateStatusAsync(id, RequestStatus.Cancelled, HttpContext.RequestAborted);
            TempData["Success"] = "درخواست لغو شد.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    private Guid? GetUserId()
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(id, out var guid) ? guid : null;
    }
}

public class CreateServiceRequestViewModel
{
    [Required(ErrorMessage = "دسته‌بندی را انتخاب کنید.")]
    public int CategoryId { get; set; }
    public int? ServiceId { get; set; }

    [Required(ErrorMessage = "عنوان درخواست الزامی است.")]
    [StringLength(200)]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "توضیحات الزامی است.")]
    [StringLength(4000)]
    public string Description { get; set; } = "";

    [StringLength(500)] public string? Address { get; set; }
    [StringLength(100)] public string? City { get; set; }
    [StringLength(20)] public string? ZipCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public UrgencyLevel Urgency { get; set; } = UrgencyLevel.Flexible;
    [DataType(DataType.Date)] public DateTime? PreferredDate { get; set; }
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public bool IsHomeOwner { get; set; } = true;

    public List<IFormFile>? Images { get; set; }

    // Display only
    public IReadOnlyList<CategoryDto> Categories { get; set; } = Array.Empty<CategoryDto>();
}
