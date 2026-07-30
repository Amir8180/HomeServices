using HomeServices.Shared.Enums;

namespace HomeServices.Shared.Dtos;

/// <summary>
/// Minimal user information returned by the Identity service and consumed by the
/// MVC application. This is the inter-service contract — no sensitive data.
/// </summary>
public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public UserType UserType { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
