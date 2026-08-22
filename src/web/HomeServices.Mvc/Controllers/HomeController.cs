using System.Diagnostics;
using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using HomeServices.Mvc.Models;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Mvc.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ICategoryService _categories;
    private readonly IExpertProfileService _experts;
    private readonly IServiceService _services;
    private readonly IServiceRequestService _requests;
    private readonly IPlatformStatsService _stats;

    public HomeController(
        ILogger<HomeController> logger,
        ICategoryService categories,
        IExpertProfileService experts,
        IServiceService services,
        IServiceRequestService requests,
        IPlatformStatsService stats)
    {
        _logger = logger;
        _categories = categories;
        _experts = experts;
        _services = services;
        _requests = requests;
        _stats = stats;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _categories.GetAllAsync(true, HttpContext.RequestAborted);
        var topExperts = await _experts.GetTopRatedAsync(6, HttpContext.RequestAborted);
        var popularServices = (await _services.GetPagedAsync(
            new ServiceFilterDto { PageSize = 6, ActiveOnly = true, SortBy = "name" },
            HttpContext.RequestAborted)).Items;
        var platformStats = await _stats.GetAsync(HttpContext.RequestAborted);

        var vm = new HomeViewModel
        {
            Categories = categories,
            TopExperts = topExperts,
            PopularServices = popularServices,
            Stats = platformStats,
        };
        return View(vm);
    }

    public async Task<IActionResult> HowItWorks()
    {
        ViewBag.Categories = await _categories.GetAllAsync(true, HttpContext.RequestAborted);
        return View();
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
        => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}

public class HomeViewModel
{
    public IReadOnlyList<CategoryDto> Categories { get; set; } = Array.Empty<CategoryDto>();
    public IReadOnlyList<ExpertProfileDto> TopExperts { get; set; } = Array.Empty<ExpertProfileDto>();
    public IReadOnlyList<ServiceDto> PopularServices { get; set; } = Array.Empty<ServiceDto>();
    public PlatformStatsDto Stats { get; set; } = new();
}
