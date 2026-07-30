using HomeServices.Shared.Enums;
using Microsoft.AspNetCore.Identity;

namespace HomeServices.Identity.Domain.Entities;

/// <summary>
/// Extended Identity user for the HomeServices platform. Adds profile fields
/// (full name, avatar, user type) on top of the standard IdentityUser properties.
/// This entity lives ONLY in the Identity microservice database.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public UserType UserType { get; set; } = UserType.Customer;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}
