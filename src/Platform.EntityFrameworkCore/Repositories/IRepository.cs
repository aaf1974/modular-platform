using Platform.EntityFrameworkCore.Entities;

namespace Platform.EntityFrameworkCore.Repositories;

/// <summary>Базовый репозиторий для чтения и изменения сущностей с типизированным идентификатором.</summary>
/// <typeparam name="TEntity">Тип сущности.</typeparam>
/// <typeparam name="TId">Тип идентификатора.</typeparam>
/// <remarks>Методы изменения не фиксируют изменения в базе данных. Сохранение выполняется прикладным сценарием.</remarks>
public interface IRepository<TEntity, TId>
    where TEntity : class, IEntityId<TId>
{
    /// <summary>Получает сущность по идентификатору с учётом глобальных фильтров EF Core.</summary>
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>Получает страницу сущностей, упорядоченную по идентификатору, с учётом глобальных фильтров EF Core.</summary>
    /// <param name="skip">Количество пропускаемых записей.</param>
    /// <param name="take">Максимальное количество записей; от 1 до 500.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task<IReadOnlyList<TEntity>> ListAsync(
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>Добавляет сущность в контекст; не сохраняет изменения.</summary>
    void Add(TEntity entity);

    /// <summary>Отмечает сущность как изменённую; не сохраняет изменения.</summary>
    void Update(TEntity entity);

    /// <summary>Удаляет сущность; для <see cref="ISoftDeletable"/> применяется мягкое удаление при сохранении.</summary>
    void Delete(TEntity entity);
}
