using HomeServices.Application.Dtos;
using HomeServices.Domain.Enums;

namespace HomeServices.Application.Interfaces;

/// <summary>
/// Application service contract for service categories (CRUD + grouped listing).
/// </summary>
public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryDto>> GetByGroupAsync(CategoryGroup group, CancellationToken cancellationToken = default);
    Task<CategoryDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryDto>> GetSubCategoriesAsync(int parentId, CancellationToken cancellationToken = default);
    Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default);
    Task<CategoryDto?> UpdateAsync(int id, UpdateCategoryDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
