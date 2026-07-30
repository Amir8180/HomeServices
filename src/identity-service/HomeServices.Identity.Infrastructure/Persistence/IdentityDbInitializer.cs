using HomeServices.Identity.Domain.Entities;
using HomeServices.Identity.Infrastructure.Data;
using HomeServices.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomeServices.Identity.Infrastructure.Persistence;

/// <summary>
/// Seeds the Identity database with three roles (Customer, Expert, Admin) and
/// a default admin user so the platform is usable on first run.
/// Idempotent: skips if data already exists.
/// </summary>
public static class IdentityDbInitializer
{
    public static async Task InitializeAsync(
        IdentityDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger? logger = null)
    {
        logger?.LogInformation("Applying Identity database migrations and seeding roles...");

        try
        {
            await context.Database.MigrateAsync();
            await SeedRolesAsync(roleManager, logger);
            await SeedAdminUserAsync(userManager, logger);
            await SeedSampleUsersAsync(userManager, logger);
            logger?.LogInformation("Identity seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "An error occurred while seeding the Identity database.");
            throw;
        }
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager, ILogger? logger)
    {
        var roles = new (string Name, string Description)[]
        {
            ("Customer", "مشتری — کاربرانی که درخواست خدمات ارسال می‌کنند"),
            ("Expert", "کارشناس — متخصصانی که پیشنهاد و خدمات ارائه می‌دهند"),
            ("Admin", "مدیر — مدیران پلتفرم با دسترسی کامل"),
        };

        foreach (var (name, desc) in roles)
        {
            if (await roleManager.RoleExistsAsync(name)) continue;
            await roleManager.CreateAsync(new ApplicationRole
            {
                Name = name,
                Description = desc,
                NormalizedName = name.ToUpperInvariant(),
            });
            logger?.LogInformation("Role '{Role}' created.", name);
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, ILogger? logger)
    {
        var adminEmail = "admin@homeservices.ir";

        if (await userManager.FindByEmailAsync(adminEmail) != null) return;

        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "مدیر سیستم",
            PhoneNumber = "09121234567",
            UserType = UserType.Admin,
            IsActive = true,
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
        };

        var result = await userManager.CreateAsync(admin, "Admin@123456");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Admin");
            logger?.LogInformation("Default admin user created: {Email}", adminEmail);
        }
        else
        {
            logger?.LogWarning("Failed to create admin user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    private static async Task SeedSampleUsersAsync(UserManager<ApplicationUser> userManager, ILogger? logger)
    {
        // Sample customer (matches the id used in main service seed data)
        var customerId = new Guid("11111111-1111-1111-1111-111111111111");
        if (await userManager.FindByIdAsync(customerId.ToString()) == null)
        {
            var customer = new ApplicationUser
            {
                Id = customerId,
                UserName = "customer@example.com",
                Email = "customer@example.com",
                FullName = "علی احمدی",
                PhoneNumber = "09121112233",
                UserType = UserType.Customer,
                IsActive = true,
                EmailConfirmed = true,
            };
            await userManager.CreateAsync(customer, "User@123456");
            await userManager.AddToRoleAsync(customer, "Customer");
            logger?.LogInformation("Sample customer user created.");
        }

        // Sample expert (matches the id used in main service seed data)
        var expertId = new Guid("A1B2C3D4-E5F6-7890-1234-567890ABCDEF");
        if (await userManager.FindByIdAsync(expertId.ToString()) == null)
        {
            var expert = new ApplicationUser
            {
                Id = expertId,
                UserName = "expert@homeservices.ir",
                Email = "expert@homeservices.ir",
                FullName = "محمد رضایی",
                PhoneNumber = "09129887766",
                UserType = UserType.Expert,
                AvatarUrl = "/uploads/experts/expert1-logo.png",
                IsActive = true,
                EmailConfirmed = true,
            };
            await userManager.CreateAsync(expert, "Expert@123456");
            await userManager.AddToRoleAsync(expert, "Expert");
            logger?.LogInformation("Sample expert user created.");
        }
    }
}
