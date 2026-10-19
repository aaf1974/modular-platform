namespace Platform.EntityFrameworkCore.Auditing;

/// <summary>Реализация контракта текущего пользователя по умолчанию.</summary>
/// <remarks>Не подменяет отсутствующего пользователя фиктивным идентификатором.</remarks>
public sealed class StubCurrentUser : ICurrentUser
{
    /// <inheritdoc />
    public string? UserId => null;
}
