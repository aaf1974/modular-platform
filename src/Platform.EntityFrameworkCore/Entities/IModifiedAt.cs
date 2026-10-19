namespace Platform.EntityFrameworkCore.Entities;

/// <summary>Определяет сущность с временем последнего изменения в UTC.</summary>
public interface IModifiedAt
{
    /// <summary>Время последнего изменения в UTC; <see langword="null"/> для ещё не изменённой сущности.</summary>
    DateTimeOffset? ModifiedAtUtc { get; set; }
}
