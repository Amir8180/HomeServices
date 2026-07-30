using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using HomeServices.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Mvc.Areas.Admin.Controllers;

/// <summary>
/// Admin category management (CRUD). Categories drive the homepage tile grid and
/// the request/expert classification. Supports nested sub-categories.
/// </summary>
public class CategoriesController : AdminControllerBase
{
    private readonly ICategoryService _categories;

    public CategoriesController(ICategoryService categories)
    {
        _categories = categories;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var categories = await _categories.GetAllAsync(false, cancellationToken);
        return View(categories);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewBag.Parents = await _categories.GetAllAsync(false, cancellationToken);
        return View(new CategoryFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel model, CancellationToken cancellationToken)
    {
        ViewBag.Parents = await _categories.GetAllAsync(false, cancellationToken);
        if (!ModelState.IsValid) return View(model);

        var dto = MapToCreate(model);
        await _categories.CreateAsync(dto, cancellationToken);
        NotifySuccess("دسته‌بندی ایجاد شد.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var category = await _categories.GetByIdAsync(id, cancellationToken);
        if (category == null) return NotFound();

        ViewBag.Parents = (await _categories.GetAllAsync(false, cancellationToken))
            .Where(c => c.Id != id); // cannot be own parent

        return View(MapToForm(category));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryFormViewModel model, CancellationToken cancellationToken)
    {
        ViewBag.Parents = (await _categories.GetAllAsync(false, cancellationToken))
            .Where(c => c.Id != id);
        if (!ModelState.IsValid) return View(model);

        var dto = MapToCreate(model);
        var updated = await _categories.UpdateAsync(id, MapToUpdate(dto), cancellationToken);
        if (updated == null) return NotFound();
        NotifySuccess("دسته‌بندی به‌روزرسانی شد.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var ok = await _categories.DeleteAsync(id, cancellationToken);
        NotifySuccess(ok ? "دسته‌بندی حذف شد." : "حذف ناموفق بود.");
        return RedirectToAction(nameof(Index));
    }

    // ---------- mapping helpers ----------
    private static CreateCategoryDto MapToCreate(CategoryFormViewModel m) => new()
    {
        Name = m.Name,
        Slug = string.IsNullOrWhiteSpace(m.Slug) ? m.Name.Trim().Replace(" ", "-") : m.Slug,
        Description = m.Description,
        Group = m.Group,
        IconUrl = m.IconUrl,
        DisplayOrder = m.DisplayOrder,
        IsActive = m.IsActive,
        ParentCategoryId = m.ParentCategoryId,
    };

    private static UpdateCategoryDto MapToUpdate(CreateCategoryDto c) => new()
    {
        Name = c.Name,
        Slug = c.Slug,
        Description = c.Description,
        Group = c.Group,
        IconUrl = c.IconUrl,
        DisplayOrder = c.DisplayOrder,
        IsActive = c.IsActive,
        ParentCategoryId = c.ParentCategoryId,
    };

    private static CategoryFormViewModel MapToForm(CategoryDto c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Slug = c.Slug,
        Description = c.Description,
        Group = c.Group,
        IconUrl = c.IconUrl,
        DisplayOrder = c.DisplayOrder,
        IsActive = c.IsActive,
        ParentCategoryId = c.ParentCategoryId,
    };
}

public class CategoryFormViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public CategoryGroup Group { get; set; } = CategoryGroup.Other;
    public string? IconUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public int? ParentCategoryId { get; set; }
}
