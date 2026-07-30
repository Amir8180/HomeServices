using System.Security.Claims;
using HomeServices.Application.Contracts;
using HomeServices.Mvc.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace HomeServices.Mvc.Extensions;

/// <summary>
/// Extension methods on IServiceCollection to register MVC-specific services:
/// the cookie authentication scheme and authorization policies.
/// </summary>
public static class HomeServicesAuthExtensions
{
    /// <summary>
    /// Registers cookie authentication. The JWT from the Identity microservice is
    /// converted into claims and stored in the auth cookie so subsequent requests
    /// are authenticated without re-calling the Identity API.
    /// </summary>
    public static IServiceCollection AddHomeServicesAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie(options =>
        {
            options.Cookie.Name = "HomeServices.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
            options.SlidingExpiration = true;
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("CustomerOnly", p => p.RequireRole("Customer"));
            options.AddPolicy("ExpertOnly", p => p.RequireRole("Expert"));
            options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
            options.AddPolicy("CustomerOrExpert", p => p.RequireRole("Customer", "Expert"));
        });

        return services;
    }

    /// <summary>
    /// Builds the claims principal from a JWT access token + the user DTO returned by
    /// the Identity service.
    /// </summary>
    public static ClaimsPrincipal BuildPrincipal(string accessToken, Shared.Dtos.UserDto user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new("UserType", user.UserType.ToString()),
            new("access_token", accessToken),
            new(ClaimTypes.Role, user.UserType.ToString()),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}
