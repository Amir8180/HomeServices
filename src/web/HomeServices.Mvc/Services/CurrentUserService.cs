using System.Security.Claims;
using HomeServices.Application.Contracts;
using HomeServices.Shared.Enums;

namespace HomeServices.Mvc.Services;

/// <summary>
/// Reads the current user's identity from the authenticated claims principal.
/// The cookie auth scheme stores the JWT-derived claims (sub, name, role, UserType)
/// so we can resolve the current user without an extra round-trip per request.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;

    public CurrentUserService(IHttpContextAccessor http) => _http = http;

    private ClaimsPrincipal? Principal => _http.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var id = Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? Principal?.FindFirst("sub")?.Value;
            return Guid.TryParse(id, out var guid) ? guid : null;
        }
    }

    public string? FullName => Principal?.FindFirst(ClaimTypes.Name)?.Value
                            ?? Principal?.FindFirst("name")?.Value;

    public string? Email => Principal?.FindFirst(ClaimTypes.Email)?.Value
                         ?? Principal?.FindFirst("email")?.Value;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;

    public UserType? UserType
    {
        get
        {
            var value = Principal?.FindFirst("UserType")?.Value;
            return Enum.TryParse<UserType>(value, out var t) ? t : null;
        }
    }
}
