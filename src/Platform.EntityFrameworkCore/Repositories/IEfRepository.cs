using Microsoft.EntityFrameworkCore;
using Platform.EntityFrameworkCore.Entities;

namespace Platform.EntityFrameworkCore.Repositories;

/// <summary>EF Core-специфичный доступ к запросам репозитория.</summary>
/// <typeparam name="TEntity">Тип сущности.</typeparam>
/// <typeparam name="TId">Тип идентификатора.</typeparam>
/// <remarks>
/// <see cref="QueryIncludingDeleted"/> вызывает <see cref="EntityFrameworkQueryableExtensions.IgnoreQueryFilters{TEntity}(IQueryable{TEntity})"/>,
/// поэтому отключает все глобальные фильтры EF Core для запроса, а не только фильтр soft delete.
/// </remarks>
public interface IEfRepository<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : class, IEntityId<TId>
{
    /// <summary>Возвращает запрос с применёнными глобальными фильтрами.</summary>
    IQueryable<TEntity> Query();

    /// <summary>Возвращает запрос без глобальных фильтров EF Core.</summary>
    IQueryable<TEntity> QueryIncludingDeleted();
}
