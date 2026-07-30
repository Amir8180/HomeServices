using HomeServices.Shared.Dtos;
using HomeServices.Shared.Enums;

namespace HomeServices.Application.Contracts;

/// <summary>
/// Client contract for the Identity microservice. Used by the MVC app to
/// register/login users and resolve user info via JWT-secured HTTP calls.
/// </summary>
public interface IIdentityApiClient
{
    Task<Result<AuthResultDto>> RegisterAsync(string fullName, string email, string phoneNumber, string password, UserType userType, CancellationToken cancellationToken = default);
    Task<Result<AuthResultDto>> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>Updates a user's display name, avatar and phone number.</summary>
    Task<bool> UpdateProfileAsync(Guid id, string fullName, string? avatarUrl, string? phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>Changes a user's password (requires current password for verification).</summary>
    Task<Result> ChangePasswordAsync(Guid id, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>Activates/deactivates a user account (admin only).</summary>
    Task<bool> ToggleUserStatusAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>Result of a successful auth call (token + user).</summary>
public class AuthResultDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public UserDto? User { get; set; }
}
