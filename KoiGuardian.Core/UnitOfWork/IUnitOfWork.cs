using Microsoft.EntityFrameworkCore;
using KoiGuardian.Core.Repository;

namespace KoiGuardian.Core.UnitOfWork;

public interface IUnitOfWork<out TContext> where TContext : DbContext, IDisposable
{
    IRepository<TEntity> GetRepository<TEntity>() where TEntity : class;

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
    
    Task<int> ExecuteSqlCommandAsync(string sql, params object[] parameters);
    
    IQueryable<T> ExecuteSqlQueryAsync<T>(string sql, params object[] parameters) where T : class;
}