using HomeServices.Application.Interfaces;
using HomeServices.Domain.Common;
using HomeServices.Infrastructure.Data;
using HomeServices.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace HomeServices.Infrastructure;

/// <summary>
/// EF Core Unit of Work. Hands out repositories on demand and commits changes
/// through a single SaveChangesAsync call, with optional transaction support.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Dictionary<Type, object> _repositories = new();

    public UnitOfWork(AppDbContext context, ILoggerFactory loggerFactory)
    {
        _context = context;
        _loggerFactory = loggerFactory;
    }

    public IRepository<T> Repository<T>() where T : BaseEntity
    {
        var type = typeof(T);
        if (!_repositories.TryGetValue(type, out var repo))
        {
            // Create Repository<T>(context, logger<Repository<T>>) via reflection.
            var repoType = typeof(Repository<>).MakeGenericType(type);

            // Use LoggerFactoryExtensions.CreateLogger<TRepository> via reflection to get ILogger<Repository<T>>
            var createLoggerMethod = typeof(Microsoft.Extensions.Logging.LoggerFactoryExtensions)
                .GetMethods()
                .First(m => m.Name == "CreateLogger" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1);

            var logger = createLoggerMethod.MakeGenericMethod(repoType).Invoke(null, new object[] { _loggerFactory })!;

            repo = Activator.CreateInstance(repoType, _context, logger)!;
            _repositories[type] = repo;
        }
        return (IRepository<T>)repo;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        => await _context.BeginTransactionAsync(cancellationToken);

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        => await _context.CommitTransactionAsync(cancellationToken);

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        => await _context.RollbackTransactionAsync(cancellationToken);

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}