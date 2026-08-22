using HomeServices.Application.Dtos;
using HomeServices.Domain.Enums;
using HomeServices.Shared.Common;

namespace HomeServices.Application.Interfaces;

/// <summary>
/// Application service contract for customer reviews/ratings of experts.
/// </summary>
public interface IReviewService
{
    Task<PagedResult<ReviewDto>> GetPagedAsync(ReviewFilterDto filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReviewDto>> GetByExpertAsync(Guid expertId, CancellationToken cancellationToken = default);
    Task<ReviewDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>The review submitted for an order, if any (one review per order).</summary>
    Task<ReviewDto?> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);

    Task<ReviewDto> CreateAsync(CreateReviewDto dto, Guid customerId, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(int id, ReviewStatus status, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
