using System.Security.Claims;
using HomeServices.Identity.Application.Dtos;
using HomeServices.Identity.Application.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomeServices.Identity.Application.Services;

/// <summary>
/// Handles registration, login and refresh-token flows backed by
/// ASP.NET Core Identity and JWT.
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<Identity.Domain.Entities.ApplicationUser> _userManager;
    private readonly SignInManager<Identity.Domain.Entities.ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<Identity.Domain.Entities.ApplicationUser> userManager,
        SignInManager<Identity.Domain.Entities.ApplicationUser> signInManager,
        ITokenService tokenService,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var user = new Identity.Domain.Entities.ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            UserType = request.UserType,
            IsActive = true,
            EmailConfirmed = true, // In production use email confirmation
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return new AuthResponse
            {
                Succeeded = false,
                Message = "Registration failed.",
                Errors = result.Errors.Select(e => e.Description).ToList(),
            };
        }

        // Assign role based on UserType
        var roleName = request.UserType.ToString();
        await _userManager.AddToRoleAsync(user, roleName);

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles.ToList());
        var refreshToken = _tokenService.GenerateRefreshToken();

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User {Email} registered as {Role}", request.Email, roleName);

        return new AuthResponse
        {
            Succeeded = true,
            Message = "Registration successful.",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = _tokenService.GetAccessTokenExpiration(),
            User = MapToUserDto(user, roles.ToList()),
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
        {
            return new AuthResponse
            {
                Succeeded = false,
                Message = "Invalid email or password.",
            };
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, request.RememberMe);
        if (!result.Succeeded)
        {
            return new AuthResponse
            {
                Succeeded = false,
                Message = result.IsLockedOut
                    ? "Account locked due to too many failed attempts."
                    : "Invalid email or password.",
            };
        }

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles.ToList());
        var refreshToken = _tokenService.GenerateRefreshToken();

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User {Email} logged in", request.Email);

        return new AuthResponse
        {
            Succeeded = true,
            Message = "Login successful.",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = _tokenService.GetAccessTokenExpiration(),
            User = MapToUserDto(user, roles.ToList()),
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        // In a production app the refresh token would be stored in the database.
        // For this implementation, we validate that the refresh token looks valid
        // and generate a new access token.
        if (string.IsNullOrEmpty(refreshToken) || refreshToken.Length < 32)
        {
            return new AuthResponse { Succeeded = false, Message = "Invalid refresh token." };
        }

        // Validate the current access token claims to find the user
        // (In production, link refresh tokens to user records in DB)
        return new AuthResponse { Succeeded = false, Message = "Refresh token expired. Please login again." };
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        // In production: find and remove refresh token from DB
        await Task.CompletedTask;
        return true;
    }

    private static Shared.Dtos.UserDto MapToUserDto(Identity.Domain.Entities.ApplicationUser user, List<string> roles)
    {
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
