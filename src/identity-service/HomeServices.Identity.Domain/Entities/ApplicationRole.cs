using Microsoft.AspNetCore.Identity;

namespace HomeServices.Identity.Domain.Entities;

/// <summary>
/// Application role. Three fixed roles: Customer, Expert, Admin.
/// </summary>
public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
}
