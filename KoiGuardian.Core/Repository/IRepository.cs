using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace KoiGuardian.Core.Repository;

public interface IRepository<TEntity> where TEntity : class
{
    #region GetAsync

    Task<List<TEntity>> GetAllAsync(Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null);

    Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate,
    CancellationToken cancellationToken = default) =>

    GetAsync(predicate, false, null, null, cancellationToken);

    Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate,
    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include,
    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy,
    CancellationToken cancellationToken = default) =>
    GetAsync(predicate, false, include, orderBy, cancellationToken);

    Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate,
    bool isEnabledTracking = false,
    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
    CancellationToken cancellationToken = default);
    #endregion



    #region FindAsync
    Task<IList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate,
  CancellationToken cancellationToken = default);

    Task<IList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate,
    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>> include,
    CancellationToken cancellationToken = default);
    Task<IList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate,
    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include,
    bool isEnabledTracking,
    CancellationToken cancellationToken = default);

    Task<IList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate,
    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy,
    CancellationToken cancellationToken = default);

    Task<IList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate,
    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>> include,
    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy,
    CancellationToken cancellationToken = default);

    Task<IList<TResult>> FindAsync<TResult>(Expression<Func<TEntity, bool>> predicate,
    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include,
    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy,
    Expression<Func<TEntity, TResult>> selector,
    CancellationToken cancellationToken = default);

    Task<IList<TResult>> FindAsync<TResult>(Expression<Func<TEntity, bool>> predicate,
    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include,
    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy,
    Expression<Func<TEntity, TResult>> selector,
    int top,
    CancellationToken cancellationToken = default);
    #endregion

    Task<PaginatedList<TEntity>> FindPaginatedAsync(Expression<Func<TEntity, bool>> predicate,
  int page,
  int numberOfItems,
  Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy,
  Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
  CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    IQueryable<TEntity> GetQueryable();

    IQueryable<TEntity> GetQueryable(Expression<Func<TEntity, bool>> predicate);

    void Insert(TEntity entity);

    void Update(TEntity entity);
    void Delete(TEntity entity);
}