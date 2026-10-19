using Platform.EntityFrameworkCore.Entities;
using Platform.EntityFrameworkCore.Repositories;

namespace Platform.EntityFrameworkCore.Services;

/// <summary>Стандартная реализация CRUD-сервиса.</summary>
/// <typeparam name="TEntity">Тип сущности.</typeparam>
/// <typeparam name="TId">Тип идентификатора.</typeparam>
public class CrudService<TEntity, TId>(IRepository<TEntity, TId> repository) : ICrudService<TEntity, TId>
    where TEntity : class, IEntityId<TId>
{
    private readonly IRepository<TEntity, TId> _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    /// <inheritdoc />
    public Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default) =>
        _repository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<TEntity>> ListAsync(
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default) =>
        _repository.ListAsync(skip, take, cancellationToken);

    /// <inheritdoc />
    public TEntity Create(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _repository.Add(entity);
        return entity;
    }

    /// <inheritdoc />
    public TEntity Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _repository.Update(entity);
        return entity;
    }

    /// <inheritdoc />
    public void Delete(TEntity entity) => _repository.Delete(entity);
}
