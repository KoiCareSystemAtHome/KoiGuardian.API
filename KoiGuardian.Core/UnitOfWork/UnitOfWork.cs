using KoiGuardian.Core.Repository;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KoiGuardian.Core.UnitOfWork;

public sealed class UnitOfWork<TContext>(TContext context, IServiceProvider serviceProvider) : IUnitOfWork<TContext>, IDisposable
  where TContext : DbContext, IDisposable
{
    private bool _disposed = false;

    private readonly TContext _dbContext = context ?? throw new ArgumentNullException(nameof(context));
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private IDbContextTransaction? _currentTransaction;

    public IRepository<TEntity> GetRepository<TEntity>() where TEntity : class
    {
        return _serviceProvider.GetRequiredService<IRepository<TEntity>>();
    }
    public Task<int> ExecuteSqlCommandAsync(string sql, params object[] parameters)
    {
        return _dbContext.Database.ExecuteSqlRawAsync(sql, parameters);
    }

    public IQueryable<T> ExecuteSqlQueryAsync<T>(string sql, params object[] parameters) where T : class
    {
        return _dbContext.Set<T>().FromSqlRaw(sql, parameters).AsQueryable();
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress.");
        }

        _currentTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }


    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No transaction in progress.");
        }
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackAsync(cancellationToken);



            throw;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No transaction in progress.");
        }


        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
    }

    public void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _dbContext.Dispose();
                _currentTransaction?.Dispose();
            }



            _disposed = true;
        }
    }
}