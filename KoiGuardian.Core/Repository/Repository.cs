using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore;
using KoiGuardian.DataAccess;
using System.Linq.Expressions;

namespace KoiGuardian.Core.Repository;


public class BaseRepository<TEntity>(DbContext dbContext) : IBaseRepository<TEntity>
  where TEntity : class
{
    public async Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate,
    bool isEnabledTracking = false,
    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
    CancellationToken cancellationToken = default) =>
    await ApplyIncludesAndOrdering(GetQueryable(isEnabledTracking), include, orderBy)
    .FirstOrDefaultAsync(predicate, cancellationToken);



    public async Task<IList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate,
    CancellationToken cancellationToken = default) =>
    await GetQueryable().Where(predicate).ToListAsync(cancellationToken);



    public async Task<IList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate,
    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>> include,
    CancellationToken cancellationToken = default) =>
    await ApplyIncludesAndOrdering(GetQueryable(), include, null)
    .Where(predicate).ToListAsync(cancellationToken);





    public async Task<IList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate,
    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include,
    bool isEnabledTracking,
    CancellationToken cancellationToken = default)
    => await ApplyIncludesAndOrdering(GetQueryable(isEnabledTracking), include, null)
    .Where(predicate).ToListAsync(cancellationToken);



    public async Task<IList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate,
    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy,
    CancellationToken cancellationToken = default) =>
    await ApplyIncludesAndOrdering(GetQueryable(), null, orderBy)
    .Where(predicate).ToListAsync(cancellationToken);



    public async Task<IList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate,
    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>> include,
    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy,
    CancellationToken cancellationToken = default) =>
    await ApplyIncludesAndOrdering(GetQueryable(), include, orderBy)
    .Where(predicate).ToListAsync(cancellationToken);



    public async Task<IList<TResult>> FindAsync<TResult>(Expression<Func<TEntity, bool>> predicate,
    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include,
    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy,
    Expression<Func<TEntity, TResult>> selector,
    CancellationToken cancellationToken = default) =>
    await ApplyIncludesAndOrdering(GetQueryable(), include, orderBy)
    .Where(predicate).Select(selector).ToListAsync(cancellationToken);



    public async Task<IList<TResult>> FindAsync<TResult>(Expression<Func<TEntity, bool>> predicate,
    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include,
    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy,
    Expression<Func<TEntity, TResult>> selector,
    int top,
    CancellationToken cancellationToken = default) =>
    await ApplyIncludesAndOrdering(GetQueryable(), include, orderBy, top)
    .Where(predicate).Select(selector).ToListAsync(cancellationToken);

    public async Task<PaginatedList<TEntity>> FindPaginatedAsync(Expression<Func<TEntity, bool>> predicate,
    int page,
    int numberOfItems,
    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy,
    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
    CancellationToken cancellationToken = default)
    {
        var queryable = ApplyIncludesAndOrdering(GetQueryable(), include, orderBy)
        .Where(predicate);



        return await PaginatedList<TEntity>.CreateAsync(queryable, page, numberOfItems, cancellationToken);
    }

    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate,
    CancellationToken cancellationToken = default) => await GetQueryable().Where(predicate).AnyAsync(cancellationToken);

    public void Insert(TEntity entity)
    {
        DbSet.Add(entity);
        dbContext.SaveChanges();
    }
    public void Update(TEntity entity)
    {
        var entry = dbContext.Entry(entity);

        if (entry.State == EntityState.Detached)
        {
            DbSet.Attach(entity);
        }



        entry.State = EntityState.Modified;
    }

    public void Delete(TEntity entity)
    {
        var entry = dbContext.Entry(entity);



        if (entry.State == EntityState.Detached)
        {
            DbSet.Attach(entity);
        }



        DbSet.Remove(entity);
    }
    public IQueryable<TEntity> GetQueryable() => GetQueryable(false);

    private IQueryable<TEntity> GetQueryable(bool isEnabledTracking)
    {
        return isEnabledTracking ? DbSet : DbSet.AsNoTracking();
    }


    private static IQueryable<TEntity> ApplyIncludesAndOrdering(IQueryable<TEntity> queryable,
    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include,
    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy, int? top = null)
    {
        if (include != null)
        {
            queryable = include(queryable);
        }



        if (orderBy != null)
        {
            queryable = orderBy(queryable);
        }



        if (top is not null)
        {
            queryable = queryable.Take(top.Value);
        }



        return queryable;
    }

    public Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return GetQueryable(predicate).CountAsync(cancellationToken);
    }

    public IQueryable<TEntity> GetQueryable(Expression<Func<TEntity, bool>> predicate)
    {
        return GetQueryable(false).Where(predicate);
    }


    public Task<List<TEntity>> GetAllAsync(Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null)
    {
        var query = GetQueryable();



        if (include != null)
        {
            query = include(query);
        }



        return query.ToListAsync();
    }

    private DbSet<TEntity> DbSet => dbContext.Set<TEntity>();
}


public interface IRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class;

public class Repository<TEntity>(KoiGuardianDbContext dbContext)
    : BaseRepository<TEntity>(dbContext), IRepository<TEntity> where TEntity : class;