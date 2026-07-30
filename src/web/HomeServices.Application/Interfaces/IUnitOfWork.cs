namespace HomeServices.Application.Interfaces;

/// <summary>
/// Unit of Work contract. Provides access to repositories and a single
/// SaveChangesAsync boundary so multiple aggregate changes commit atomically.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IRepository<T> Repository<T>() where T : Domain.Common.BaseEntity;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
