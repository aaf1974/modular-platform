namespace Platform.EntityFrameworkCore.Entities;

/// <summary>Определяет сущность с идентификатором последнего изменившего пользователя.</summary>
public interface IModifiedBy
{
    /// <summary>Идентификатор последнего изменившего пользователя или <see langword="null"/>.</summary>
    string? ModifiedByUserId { get; set; }
}
