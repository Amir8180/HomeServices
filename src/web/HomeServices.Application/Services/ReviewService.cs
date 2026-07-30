using AutoMapper;
using HomeServices.Application.Common;
using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using HomeServices.Domain.Entities;
using HomeServices.Domain.Enums;
using HomeServices.Shared.Common;
using Microsoft.Extensions.Logging;

namespace HomeServices.Application.Services;

/// <summary>
/// Application service for customer reviews/ratings. Creating a review verifies the
/// caller owns the completed order, rejects duplicates, and (when approved by an
/// admin) recalculates the expert's rolling average rating and review count.
/// </summary>
public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(IUnitOfWork uow, IMapper mapper, ICacheService cache, ILogger<ReviewService> logger)
    {
        _uow = uow; _mapper = mapper; _cache = cache; _logger = logger;
    }

    public async Task<PagedResult<ReviewDto>> GetPagedAsync(ReviewFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _uow.Repository<Review>().GetAllNoTracking().AsQueryable();

        if (filter.ExpertId.HasValue) query = query.Where(r => r.ExpertId == filter.ExpertId);
        if (filter.CustomerId.HasValue) query = query.Where(r => r.CustomerId == filter.CustomerId);
        if (filter.Status.HasValue) query = query.Where(r => r.Status == filter.Status);
        if (filter.MinRating.HasValue) query = query.Where(r => r.Rating >= filter.MinRating);
        if (filter.MaxRating.HasValue) query = query.Where(r => r.Rating <= filter.MaxRating);

        query = query.OrderByDescending(r => r.CreatedAt);

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize is < 1 or > 100 ? 12 : filter.PageSize;
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<ReviewDto>
        {
            Items = _mapper.Map<List<ReviewDto>>(items),
            TotalCount = total,
            PageNumber = page,
            PageSize = pageSize,
        };
    }

    public async Task<IReadOnlyList<ReviewDto>> GetByExpertAsync(Guid expertId, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.Reviews.ByExpert(expertId);
        return await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var list = await _uow.Repository<Review>().GetAllNoTracking()
                .Where(r => r.ExpertId == expertId && r.Status == ReviewStatus.Approved)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(cancellationToken);
            return _mapper.Map<List<ReviewDto>>(list);
        }, TimeSpan.FromMinutes(15), cancellationToken);
    }

    public async Task<ReviewDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Repository<Review>().GetByIdAsync(id, cancellationToken);
        return entity == null ? null : _mapper.Map<ReviewDto>(entity);
    }

    public async Task<ReviewDto> CreateAsync(CreateReviewDto dto, Guid customerId, CancellationToken cancellationToken = default)
    {
        // Validate ownership + completion.
        var order = await _uow.Repository<Order>().GetByIdAsync(dto.OrderId, cancellationToken);
        if (order == null) throw new InvalidOperationException("Order not found.");
        if (order.CustomerId != customerId) throw new InvalidOperationException("You can only review your own orders.");
        if (order.Status != OrderStatus.Completed)
            throw new InvalidOperationException("You can only review completed orders.");

        // No duplicate reviews per order.
        var dup = await _uow.Repository<Review>().AnyAsync(r => r.OrderId == dto.OrderId, cancellationToken);
        if (dup) throw new InvalidOperationException("This order has already been reviewed.");

        if (dto.Rating is < 1 or > 5) throw new InvalidOperationException("Rating must be between 1 and 5.");

        var review = _mapper.Map<Review>(dto);
        review.CustomerId = customerId;
        review.ExpertId = order.ExpertId;
        review.RequestId = order.RequestId;
        review.Status = ReviewStatus.Approved; // auto-approve; admins can reject via the panel
        review.IsVerified = true;
        review.CreatedBy = customerId;

        await _uow.Repository<Review>().AddAsync(review, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        // Recalculate the expert's rolling rating immediately on approval.
        await RecalculateExpertRatingAsync(order.ExpertId, cancellationToken);
        await _cache.RemoveAsync(CacheKeys.Reviews.ByExpert(order.ExpertId), cancellationToken);

        _logger.LogInformation("Review {Id} created by {Customer} for expert {Expert}.", review.Id, customerId, order.ExpertId);
        return _mapper.Map<ReviewDto>(review);
    }

    public async Task<bool> UpdateStatusAsync(int id, ReviewStatus status, CancellationToken cancellationToken = default)
    {
        var review = await _uow.Repository<Review>().GetByIdAsync(id, cancellationToken);
        if (review == null) return false;

        var previous = review.Status;
        review.Status = status;
        _uow.Repository<Review>().Update(review);
        await _uow.SaveChangesAsync(cancellationToken);

        // When approval state changes, recompute the expert aggregate.
        if (previous != status && (previous == ReviewStatus.Approved || status == ReviewStatus.Approved))
        {
            await RecalculateExpertRatingAsync(review.ExpertId, cancellationToken);
            await _cache.RemoveAsync(CacheKeys.Reviews.ByExpert(review.ExpertId), cancellationToken);
        }

        _logger.LogInformation("Review {Id} status -> {Status}.", id, status);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var review = await _uow.Repository<Review>().GetByIdAsync(id, cancellationToken);
        if (review == null) return false;
        _uow.Repository<Review>().SoftDelete(review);
        await _uow.SaveChangesAsync(cancellationToken);

        await RecalculateExpertRatingAsync(review.ExpertId, cancellationToken);
        await _cache.RemoveAsync(CacheKeys.Reviews.ByExpert(review.ExpertId), cancellationToken);
        return true;
    }

    private async Task RecalculateExpertRatingAsync(Guid expertUserId, CancellationToken cancellationToken)
    {
        var expert = await _uow.Repository<ExpertProfile>()
            .GetAllNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == expertUserId, cancellationToken);
        if (expert == null) return;

        var stats = await _uow.Repository<Review>().GetAllNoTracking()
            .Where(r => r.ExpertId == expertUserId && r.Status == ReviewStatus.Approved)
            .GroupBy(r => r.ExpertId)
            .Select(g => new { Avg = g.Average(r => r.Rating), Count = g.Count() })
            .FirstOrDefaultAsync(cancellationToken);

        expert.RatingAverage = stats?.Avg ?? 0;
        expert.ReviewCount = stats?.Count ?? 0;
        _uow.Repository<ExpertProfile>().Update(expert);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
