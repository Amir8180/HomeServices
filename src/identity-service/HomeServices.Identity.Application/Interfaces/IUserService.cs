using HomeServices.Identity.Application.Dtos;
using HomeServices.Shared.Dtos;

namespace HomeServices.Identity.Application.Interfaces;

/// <summary>
/// User management contract (profile, password, listing).
/// </summary>
public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> UpdateProfileAsync(Guid id, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<bool> ChangePasswordAsync(Guid id, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<bool> ToggleUserStatusAsync(Guid id, CancellationToken cancellationToken = default);
}
