using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Mvc.Controllers;

public class ServicesController : Controller
{
    private readonly IServiceService _services;
    private readonly ICategoryService _categories;

    public ServicesController(IServiceService services, ICategoryService categories)
    {
        _services = services;
        _categories = categories;
    }

    public async Task<IActionResult> Index(
        string? search = null,
        int? categoryId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? sortBy = null,
        int page = 1)
    {
        var filter = new ServiceFilterDto
        {
            SearchTerm = search,
            CategoryId = categoryId,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            SortBy = sortBy,
            Page = page,
            PageSize = 12,
            ActiveOnly = true,
        };

        var result = await _services.GetPagedAsync(filter, HttpContext.RequestAborted);
        ViewBag.Categories = await _categories.GetAllAsync(true, HttpContext.RequestAborted);
        ViewBag.CurrentFilter = filter;

        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        var service = await _services.GetByIdAsync(id, HttpContext.RequestAborted);
        if (service == null) return NotFound();
        return View(service);
    }

    public async Task<IActionResult> ByCategory(int id)
    {
        var services = await _services.GetByCategoryAsync(id, HttpContext.RequestAborted);
        var category = await _categories.GetByIdAsync(id, HttpContext.RequestAborted);
        ViewBag.Category = category;
        return View(services);
    }
}
