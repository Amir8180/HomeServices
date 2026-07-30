using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Mvc.Areas.Admin.Controllers;

/// <summary>
/// Admin service catalogue management (CRUD). Services are the billable items
/// customers request; this controller keeps the catalogue accurate and priced.
/// </summary>
public class ServicesController : AdminControllerBase
{
    private readonly IServiceService _services;
    private readonly ICategoryService _categories;

    public ServicesController(IServiceService services, ICategoryService categories)
    {
        _services = services;
        _categories = categories;
    }

    public async Task<IActionResult> Index(
        int? categoryId = null, string? search = null, int page = 1, CancellationToken cancellationToken = default)
    {
        var filter = new ServiceFilterDto
        {
            CategoryId = categoryId,
            SearchTerm = search,
            Page = page,
            PageSize = 20,
            ActiveOnly = false, // admins see inactive items too
        };

        var result = await _services.GetPagedAsync(filter, cancellationToken);
        ViewBag.Categories = await _categories.GetAllAsync(false, cancellationToken);
        ViewBag.CurrentFilter = filter;
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewBag.Categories = await _categories.GetAllAsync(true, cancellationToken);
        return View(new ServiceFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceFormViewModel model, CancellationToken cancellationToken)
    {
        ViewBag.Categories = await _categories.GetAllAsync(true, cancellationToken);
        if (!ModelState.IsValid) return View(model);

        await _services.CreateAsync(MapToCreate(model), cancellationToken);
        NotifySuccess("خدمت ایجاد شد.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var service = await _services.GetByIdAsync(id, cancellationToken);
        if (service == null) return NotFound();

        ViewBag.Categories = await _categories.GetAllAsync(true, cancellationToken);
        return View(MapToForm(service));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ServiceFormViewModel model, CancellationToken cancellationToken)
    {
        ViewBag.Categories = await _categories.GetAllAsync(true, cancellationToken);
        if (!ModelState.IsValid) return View(model);

        var updated = await _services.UpdateAsync(id, MapToUpdate(MapToCreate(model)), cancellationToken);
        if (updated == null) return NotFound();
        NotifySuccess("خدمت به‌روزرسانی شد.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var ok = await _services.DeleteAsync(id, cancellationToken);
        NotifySuccess(ok ? "خدمت حذف شد." : "حذف ناموفق بود.");
        return RedirectToAction(nameof(Index));
    }

    // ---------- mapping helpers ----------
    private static CreateServiceDto MapToCreate(ServiceFormViewModel m) => new()
    {
        Title = m.Title,
        Slug = string.IsNullOrWhiteSpace(m.Slug) ? m.Title.Trim().Replace(" ", "-") : m.Slug,
        Description = m.Description,
        CategoryId = m.CategoryId,
        BasePrice = m.BasePrice,
        IconUrl = m.IconUrl,
        ThumbnailUrl = m.ThumbnailUrl,
        EstimatedDurationMinutes = m.EstimatedDurationMinutes,
        IsFixedPrice = m.IsFixedPrice,
        DisplayOrder = m.DisplayOrder,
        IsActive = m.IsActive,
    };

    private static UpdateServiceDto MapToUpdate(CreateServiceDto s) => new()
    {
        Title = s.Title,
        Slug = s.Slug,
        Description = s.Description,
        CategoryId = s.CategoryId,
        BasePrice = s.BasePrice,
        IconUrl = s.IconUrl,
        ThumbnailUrl = s.ThumbnailUrl,
        EstimatedDurationMinutes = s.EstimatedDurationMinutes,
        IsFixedPrice = s.IsFixedPrice,
        DisplayOrder = s.DisplayOrder,
        IsActive = s.IsActive,
    };

    private static ServiceFormViewModel MapToForm(ServiceDto s) => new()
    {
        Id = s.Id,
        Title = s.Title,
        Slug = s.Slug,
        Description = s.Description,
        CategoryId = s.CategoryId,
        BasePrice = s.BasePrice,
        IconUrl = s.IconUrl,
        ThumbnailUrl = s.ThumbnailUrl,
        EstimatedDurationMinutes = s.EstimatedDurationMinutes,
        IsFixedPrice = s.IsFixedPrice,
        DisplayOrder = s.DisplayOrder,
        IsActive = s.IsActive,
    };
}

public class ServiceFormViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public decimal? BasePrice { get; set; }
    public string? IconUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public bool IsFixedPrice { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
