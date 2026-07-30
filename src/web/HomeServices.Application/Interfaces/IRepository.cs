using System.Linq.Expressions;
using HomeServices.Domain.Common;

namespace HomeServices.Application.Interfaces;

/// <summary>
/// Generic async repository contract (Repository pattern + ISP). Exposes queryable
/// access plus the standard CRUD operations used across all aggregates. Concrete
/// implementations live in Infrastructure.
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    // ----- Queries -----
    IQueryable<T> GetAll();
    IQueryable<T> GetAllNoTracking();

    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    // ----- Commands -----
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    void Update(T entity);

    void UpdateRange(IEnumerable<T> entities);

    void SoftDelete(T entity);

    void HardDelete(T entity);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
