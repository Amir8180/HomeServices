using HomeServices.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Identity.Infrastructure.Data;

/// <summary>
/// Separate EF Core DbContext for the Identity microservice. Owns all
/// ASP.NET Core Identity tables (Users, Roles, Claims, Logins, Tokens).
/// Runs on its own SQL Server database (HomeServices.IdentityDb).
/// </summary>
public class IdentityDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Customize ASP.NET Identity table names (optional — prefix with HS_)
        builder.Entity<ApplicationUser>(b =>
        {
            b.ToTable("Users");
            b.Property(u => u.FullName).HasMaxLength(200).IsRequired();
            b.Property(u => u.AvatarUrl).HasMaxLength(500);
            b.HasIndex(u => u.Email).IsUnique();
            b.HasIndex(u => u.PhoneNumber);
        });

        builder.Entity<ApplicationRole>(b =>
        {
            b.ToTable("Roles");
            b.Property(r => r.Description).HasMaxLength(300);
        });

        builder.Entity<IdentityUserClaim<Guid>>(b => b.ToTable("UserClaims"));
        builder.Entity<IdentityUserLogin<Guid>>(b => b.ToTable("UserLogins"));
        builder.Entity<IdentityUserToken<Guid>>(b => b.ToTable("UserTokens"));
        builder.Entity<IdentityRoleClaim<Guid>>(b => b.ToTable("RoleClaims"));
        builder.Entity<IdentityUserRole<Guid>>(b => b.ToTable("UserRoles"));
    }
}
