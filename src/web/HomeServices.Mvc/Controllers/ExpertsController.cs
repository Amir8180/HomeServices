using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using HomeServices.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Mvc.Controllers;

[AllowAnonymous]
public class ExpertsController : Controller
{
    private readonly IExpertProfileService _experts;
    private readonly IReviewService _reviews;
    private readonly ICategoryService _categories;

    public ExpertsController(
        IExpertProfileService experts,
        IReviewService reviews,
        ICategoryService categories)
    {
        _experts = experts;
        _reviews = reviews;
        _categories = categories;
    }

    public async Task<IActionResult> Index(
        string? search = null, int? categoryId = null, string? city = null, int page = 1)
    {
        var filter = new ExpertProfileFilterDto
        {
            SearchTerm = search,
            CategoryId = categoryId,
            City = city,
            ActiveOnly = true,
            IsApproved = true,
            Page = page,
            PageSize = 12,
        };

        var result = await _experts.GetPagedAsync(filter, HttpContext.RequestAborted);
        ViewBag.Categories = await _categories.GetAllAsync(true, HttpContext.RequestAborted);
        ViewBag.CurrentFilter = filter;
        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        var expert = await _experts.GetByIdAsync(id, HttpContext.RequestAborted);
        if (expert == null) return NotFound();

        var reviews = await _reviews.GetByExpertAsync(expert.UserId, HttpContext.RequestAborted);
        ViewBag.Reviews = reviews;
        return View(expert);
    }
}
