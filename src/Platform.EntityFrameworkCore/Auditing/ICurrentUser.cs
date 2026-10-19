namespace Platform.EntityFrameworkCore.Auditing;

/// <summary>Предоставляет идентификатор текущего пользователя для аудита изменений.</summary>
public interface ICurrentUser
{
    /// <summary>Идентификатор текущего пользователя или <see langword="null"/>, если он не определён.</summary>
    string? UserId { get; }
}
