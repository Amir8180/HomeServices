using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using HomeServices.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Mvc.Areas.Admin.Controllers;

/// <summary>
/// Admin dashboard: aggregate counts across requests, orders, experts, reviews
/// and revenue to give managers a single overview landing page.
/// </summary>
public class DashboardController : AdminControllerBase
{
    private readonly IServiceRequestService _requests;
    private readonly IOrderService _orders;
    private readonly IExpertProfileService _experts;
    private readonly IReviewService _reviews;
    private readonly ICategoryService _categories;
    private readonly IServiceService _services;

    public DashboardController(
        IServiceRequestService requests,
        IOrderService orders,
        IExpertProfileService experts,
        IReviewService reviews,
        ICategoryService categories,
        IServiceService services)
    {
        _requests = requests;
        _orders = orders;
        _experts = experts;
        _reviews = reviews;
        _categories = categories;
        _services = services;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var openRequests = await _requests.GetPagedAsync(
            new ServiceRequestFilterDto { Status = RequestStatus.Open, PageSize = 1 }, cancellationToken);
        var inProgressRequests = await _requests.GetPagedAsync(
            new ServiceRequestFilterDto { Status = RequestStatus.InProgress, PageSize = 1 }, cancellationToken);

        var completedOrders = await _orders.GetPagedAsync(
            new OrderFilterDto { Status = OrderStatus.Completed, PageSize = 1 }, cancellationToken);
        var activeOrders = await _orders.GetPagedAsync(
            new OrderFilterDto { PageSize = 1 }, cancellationToken);

        var pendingReviews = await _reviews.GetPagedAsync(
            new ReviewFilterDto { Status = ReviewStatus.Pending, PageSize = 1 }, cancellationToken);

        var pendingExperts = await _experts.GetPagedAsync(
            new ExpertProfileFilterDto { IsApproved = false, ActiveOnly = false, PageSize = 1 }, cancellationToken);

        var services = await _services.GetPagedAsync(new ServiceFilterDto { PageSize = 1 }, cancellationToken);
        var categories = await _categories.GetAllAsync(false, cancellationToken);

        ViewBag.OpenRequestsCount = openRequests.TotalCount;
        ViewBag.InProgressRequestsCount = inProgressRequests.TotalCount;
        ViewBag.CompletedOrdersCount = completedOrders.TotalCount;
        ViewBag.TotalOrdersCount = activeOrders.TotalCount;
        ViewBag.PendingReviewsCount = pendingReviews.TotalCount;
        ViewBag.PendingExpertsCount = pendingExperts.TotalCount;
        ViewBag.ServicesCount = services.TotalCount;
        ViewBag.CategoriesCount = categories.Count;

        // Revenue from completed orders.
        var completedList = completedOrders.Items;
        ViewBag.TotalRevenue = completedList.Sum(o => o.TotalAmount);

        return View();
    }
}
