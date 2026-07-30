using HomeServices.Application.Dtos;
using HomeServices.Shared.Common;

namespace HomeServices.Application.Interfaces;

/// <summary>
/// Application service contract for services (the catalogue shown to customers).
/// </summary>
public interface IServiceService
{
    Task<PagedResult<ServiceDto>> GetPagedAsync(ServiceFilterDto filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceDto>> GetByCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<ServiceDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<ServiceDto> CreateAsync(CreateServiceDto dto, CancellationToken cancellationToken = default);
    Task<ServiceDto?> UpdateAsync(int id, UpdateServiceDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
