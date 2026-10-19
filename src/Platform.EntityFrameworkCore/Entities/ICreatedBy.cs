namespace Platform.EntityFrameworkCore.Entities;

/// <summary>Определяет сущность с идентификатором создавшего пользователя.</summary>
public interface ICreatedBy
{
    /// <summary>Идентификатор создавшего пользователя или <see langword="null"/>.</summary>
    string? CreatedByUserId { get; set; }
}
