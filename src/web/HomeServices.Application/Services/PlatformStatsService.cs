using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using HomeServices.Domain.Entities;
using HomeServices.Domain.Enums;
using HomeServices.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomeServices.Application.Services;

/// <summary>
/// Computes platform statistics (verified experts, completed projects, rating and
/// satisfaction) directly from the database. Results are cached for a short period
/// because these numbers appear on the public homepage.
/// </summary>
public class PlatformStatsService : IPlatformStatsService
{
    private readonly IUnitOfWork _uow;
    private readonly ICacheService _cache;
    private readonly ILogger<PlatformStatsService> _logger;

    private const string CacheKey = "platform-stats";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    public PlatformStatsService(IUnitOfWork uow, ICacheService cache, ILogger<PlatformStatsService> logger)
    {
        _uow = uow;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PlatformStatsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetAsync<PlatformStatsDto>(CacheKey, cancellationToken);
        if (cached != null) return cached;

        var stats = await ComputeAsync(cancellationToken);
        await _cache.SetAsync(CacheKey, stats, CacheTtl, cancellationToken);
        return stats;
    }

    private async Task<PlatformStatsDto> ComputeAsync(CancellationToken cancellationToken)
    {
        // کارشناسان فعال و تأییدشده
        var verifiedExperts = await _uow.Repository<ExpertProfile>().GetAllNoTracking()
            .Where(e => !e.IsDeleted && e.IsActive && e.IsApproved)
            .CountAsync(cancellationToken);

        // سفارش‌های تکمیل‌شده = پروژه‌های انجام‌شده
        var completedProjects = await _uow.Repository<Order>().GetAllNoTracking()
            .Where(o => !o.IsDeleted && o.Status == OrderStatus.Completed)
            .CountAsync(cancellationToken);

        // نظرات تأییدشده: میانگین امتیاز و درصد رضایت (نظرات ۴ و ۵ ستاره)
        var reviewsQuery = _uow.Repository<Review>().GetAllNoTracking()
            .Where(r => !r.IsDeleted && r.Status == ReviewStatus.Approved);

        var totalReviews   = await reviewsQuery.CountAsync(cancellationToken);
        var satisfiedCount = await reviewsQuery.CountAsync(r => r.Rating >= 4, cancellationToken);
        var avgRating      = totalReviews > 0
            ? await reviewsQuery.AverageAsync(r => (double)r.Rating, cancellationToken)
            : 0d;

        var stats = new PlatformStatsDto
        {
            VerifiedExperts    = verifiedExperts,
            CompletedProjects  = completedProjects,
            TotalReviews       = totalReviews,
            AverageRating      = Math.Round(avgRating, 1),
            SatisfactionPercent = totalReviews > 0
                ? (int)Math.Round(satisfiedCount * 100.0 / totalReviews)
                : 0,
            TotalSiteRevenue   = await _uow.Repository<ExpertPayout>().GetAllNoTracking()
                .SumAsync(p => p.CommissionAmount, cancellationToken),
        };

        _logger.LogInformation(
            "Platform stats computed: {Experts} experts, {Completed} completed, {Reviews} reviews, avg {Avg}",
            stats.VerifiedExperts, stats.CompletedProjects, stats.TotalReviews, stats.AverageRating);

        return stats;
    }

    public Task InvalidateCacheAsync(CancellationToken cancellationToken = default)
        => _cache.RemoveAsync(CacheKey, cancellationToken);
}
