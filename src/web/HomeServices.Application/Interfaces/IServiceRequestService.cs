using HomeServices.Application.Dtos;
using HomeServices.Domain.Enums;
using HomeServices.Shared.Common;

namespace HomeServices.Application.Interfaces;

/// <summary>
/// Application service contract for customer service requests.
/// </summary>
public interface IServiceRequestService
{
    Task<PagedResult<ServiceRequestDto>> GetPagedAsync(ServiceRequestFilterDto filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceRequestDto>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceRequestDto>> GetByCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<ServiceRequestDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceRequestDto> CreateAsync(CreateServiceRequestDto dto, Guid customerId, CancellationToken cancellationToken = default);
    Task<ServiceRequestDto?> UpdateAsync(int id, UpdateServiceRequestDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(int id, RequestStatus status, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Attaches an image to a request. Returns false if the request does not exist.</summary>
    Task<bool> AddImageAsync(int requestId, string imageUrl, string? thumbnailUrl, CancellationToken cancellationToken = default);
}
