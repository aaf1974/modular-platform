namespace Platform.EntityFrameworkCore.Entities;

/// <summary>Определяет сущность с временем создания в UTC.</summary>
public interface ICreatedAt
{
    /// <summary>Время создания в UTC.</summary>
    DateTimeOffset CreatedAtUtc { get; set; }
}
