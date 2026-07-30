using HomeServices.Application.Dtos;
using HomeServices.Shared.Common;

namespace HomeServices.Application.Interfaces;

/// <summary>
/// Application service contract for expert (professional) profiles.
/// </summary>
public interface IExpertProfileService
{
    Task<PagedResult<ExpertProfileDto>> GetPagedAsync(ExpertProfileFilterDto filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpertProfileDto>> GetTopRatedAsync(int count = 6, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpertProfileDto>> GetByCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<ExpertProfileDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ExpertProfileDto?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ExpertProfileDto> CreateAsync(CreateExpertProfileDto dto, CancellationToken cancellationToken = default);
    Task<ExpertProfileDto?> UpdateAsync(int id, UpdateExpertProfileDto dto, CancellationToken cancellationToken = default);
    Task<bool> ApproveAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the expert profile owned by the given user, or null when the user
    /// has not created one yet. Used by the expert dashboard to detect onboarding.
    /// </summary>
    Task<bool> ExistsForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    // -------------------- Portfolio management --------------------
    /// <summary>Adds a portfolio image to the expert's profile.</summary>
    Task<bool> AddPortfolioImageAsync(Guid userId, string imageUrl, string? thumbnailUrl, string? title, CancellationToken cancellationToken = default);

    /// <summary>Removes a portfolio image owned by the expert.</summary>
    Task<bool> DeletePortfolioImageAsync(int portfolioImageId, Guid userId, CancellationToken cancellationToken = default);
}
