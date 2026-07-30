using HomeServices.Application.Dtos;
using HomeServices.Domain.Enums;
using HomeServices.Shared.Common;

namespace HomeServices.Application.Interfaces;

/// <summary>
/// Application service contract for orders created from accepted proposals.
/// </summary>
public interface IOrderService
{
    Task<PagedResult<OrderDto>> GetPagedAsync(OrderFilterDto filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderDto>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderDto>> GetByExpertAsync(Guid expertId, CancellationToken cancellationToken = default);
    Task<OrderDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<OrderDto?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);

    /// <summary>Creates an order from an accepted proposal and returns it.</summary>
    Task<OrderDto> CreateFromProposalAsync(int proposalId, Guid customerId, CancellationToken cancellationToken = default);

    Task<OrderDto?> UpdateStatusAsync(int id, OrderStatus status, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
