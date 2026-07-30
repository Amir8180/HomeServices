using System.Reflection;
using HomeServices.Domain.Common;
using HomeServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HomeServices.Infrastructure.Data;

/// <summary>
/// EF Core DbContext for the main HomeServices service. Owns all aggregates
/// (catalogue, requests, proposals, orders, experts, reviews, media, settings).
/// User identity lives in the separate Identity microservice; here users are
/// referenced only by their Guid id.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServiceImage> ServiceImages => Set<ServiceImage>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<RequestImage> RequestImages => Set<RequestImage>();
    public DbSet<Proposal> Proposals => Set<Proposal>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ExpertProfile> ExpertProfiles => Set<ExpertProfile>();
    public DbSet<ExpertCategory> ExpertCategories => Set<ExpertCategory>();
    public DbSet<ExpertService> ExpertServices => Set<ExpertService>();
    public DbSet<ExpertPortfolioImage> ExpertPortfolioImages => Set<ExpertPortfolioImage>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<Media> Media => Set<Media>();
    public DbSet<Notification> Notifications => Set<Notification>();

    private IDbContextTransaction? _currentTransaction;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all IEntityTypeConfiguration<T> from this assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global soft-delete query filter for all entities deriving from BaseEntity.
        ApplySoftDeleteQueryFilter(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Applies a global query filter (e => !e.IsDeleted) to every entity derived from
    /// BaseEntity using the strongly-typed generic HasQueryFilter API via reflection.
    /// </summary>
    private static void ApplySoftDeleteQueryFilter(ModelBuilder modelBuilder)
    {
        var mi = typeof(AppDbContext)
            .GetMethod(nameof(ConfigureSoftDeleteFilter), BindingFlags.Static | BindingFlags.NonPublic)!;

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                mi.MakeGenericMethod(entityType.ClrType).Invoke(null, new object[] { modelBuilder });
            }
        }
    }

    private static void ConfigureSoftDeleteFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : BaseEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
    }

    public async Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null) return null;
        _currentTransaction = await Database.BeginTransactionAsync(cancellationToken);
        return _currentTransaction;
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentTransaction != null)
                await _currentTransaction.CommitAsync(cancellationToken);
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentTransaction != null)
                await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Auto-maintain audit timestamps.
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
