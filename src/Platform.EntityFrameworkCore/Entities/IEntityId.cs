namespace Platform.EntityFrameworkCore.Entities;

/// <summary>Определяет сущность с идентификатором заданного типа.</summary>
/// <typeparam name="TId">Тип идентификатора сущности.</typeparam>
public interface IEntityId<TId>
{
    /// <summary>Идентификатор сущности.</summary>
    TId Id { get; set; }
}
