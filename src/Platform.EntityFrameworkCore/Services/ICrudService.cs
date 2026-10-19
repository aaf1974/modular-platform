using Platform.EntityFrameworkCore.Entities;

namespace Platform.EntityFrameworkCore.Services;

/// <summary>Базовый CRUD-сервис, делегирующий операции репозиторию.</summary>
/// <typeparam name="TEntity">Тип сущности.</typeparam>
/// <typeparam name="TId">Тип идентификатора.</typeparam>
/// <remarks>Сервис не вызывает SaveChanges; фиксация изменений остаётся обязанностью прикладного сценария.</remarks>
public interface ICrudService<TEntity, TId>
    where TEntity : class, IEntityId<TId>
{
    /// <summary>Получает сущность по идентификатору.</summary>
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>Получает страницу сущностей.</summary>
    Task<IReadOnlyList<TEntity>> ListAsync(
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>Добавляет сущность в текущую единицу работы.</summary>
    TEntity Create(TEntity entity);

    /// <summary>Отмечает сущность как изменённую.</summary>
    TEntity Update(TEntity entity);

    /// <summary>Отмечает сущность как удалённую.</summary>
    void Delete(TEntity entity);
}
