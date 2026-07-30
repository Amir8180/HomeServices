using HomeServices.Identity.Application.Dtos;
using HomeServices.Identity.Application.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomeServices.Identity.Application.Services;

/// <summary>
/// User management: profile retrieval, update, password change, activation toggle.
/// </summary>
public class UserService : IUserService
{
    private readonly UserManager<Identity.Domain.Entities.ApplicationUser> _userManager;
    private readonly ILogger<UserService> _logger;

    public UserService(
        UserManager<Identity.Domain.Entities.ApplicationUser> userManager,
        ILogger<UserService> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Shared.Dtos.UserDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        return user == null ? null : await MapToUserDtoAsync(user);
    }

    public async Task<Shared.Dtos.UserDto?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user == null ? null : await MapToUserDtoAsync(user);
    }

    public async Task<IReadOnlyList<Shared.Dtos.UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users.ToListAsync(cancellationToken);
        var result = new List<Shared.Dtos.UserDto>();
        foreach (var user in users)
        {
            result.Add(await MapToUserDtoAsync(user));
        }
        return result;
    }

    public async Task<bool> UpdateProfileAsync(Guid id, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return false;

        user.FullName = request.FullName;
        user.AvatarUrl = request.AvatarUrl;
        if (request.PhoneNumber != null)
            user.PhoneNumber = request.PhoneNumber;

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> ChangePasswordAsync(Guid id, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return false;

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        return result.Succeeded;
    }

    public async Task<bool> ToggleUserStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return false;

        user.IsActive = !user.IsActive;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    private async Task<Shared.Dtos.UserDto> MapToUserDtoAsync(Identity.Domain.Entities.ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new Shared.Dtos.UserDto
        {
            Id = user.Id.ToString(),
            FullName = user.FullName,
            Email = user.Email ?? "",
            PhoneNumber = user.PhoneNumber,
            AvatarUrl = user.AvatarUrl,
            UserType = user.UserType,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
        };
    }
}
