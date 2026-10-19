using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Platform.EntityFrameworkCore.Entities;

namespace Platform.EntityFrameworkCore.Repositories;

/// <summary>Стандартная реализация репозитория на основе <see cref="ApplicationDbContext"/>.</summary>
/// <typeparam name="TEntity">Тип сущности.</typeparam>
/// <typeparam name="TId">Тип идентификатора.</typeparam>
public class EfRepository<TEntity, TId>(ApplicationDbContext context) : IEfRepository<TEntity, TId>
    where TEntity : class, IEntityId<TId>
{
    private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc />
    public IQueryable<TEntity> Query() => _context.Set<TEntity>();

    /// <inheritdoc />
    public IQueryable<TEntity> QueryIncludingDeleted() => _context.Set<TEntity>().IgnoreQueryFilters();

    /// <inheritdoc />
    public Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        var entity = Expression.Parameter(typeof(TEntity), "entity");
        var entityId = Expression.Call(
            typeof(EF),
            nameof(EF.Property),
            [typeof(TId)],
            entity,
            Expression.Constant(nameof(IEntityId<TId>.Id)));
        var equality = Expression.Equal(entityId, Expression.Constant(id, typeof(TId)));
        var predicate = Expression.Lambda<Func<TEntity, bool>>(equality, entity);
        return _context.Set<TEntity>().FirstOrDefaultAsync(predicate, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TEntity>> ListAsync(
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip), skip, "Значение не может быть отрицательным.");
        if (take is < 1 or > 500) throw new ArgumentOutOfRangeException(nameof(take), take, "Допустимый диапазон: от 1 до 500.");

        return await _context.Set<TEntity>()
            .OrderBy(entity => EF.Property<TId>(entity, nameof(IEntityId<TId>.Id)))
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Add(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _context.Set<TEntity>().Add(entity);
    }

    /// <inheritdoc />
    public void Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var entry = _context.Entry(entity);
        if (entry.State == EntityState.Detached)
        {
            _context.Set<TEntity>().Attach(entity);
            entry.State = EntityState.Modified;
        }
        else if (entry.State != EntityState.Added)
        {
            entry.State = EntityState.Modified;
        }
    }

    /// <inheritdoc />
    public void Delete(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _context.Set<TEntity>().Remove(entity);
    }
}
