namespace Authentication.Abstractions;

/// <summary>
/// Выполняет восстановление пароля пользователя.
/// </summary>
public interface IPasswordResetService
{
    /// <summary>
    /// Создаёт запрос на восстановление пароля.
    /// Ответ не должен раскрывать существование учётной записи.
    /// </summary>
    Task RequestResetAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Устанавливает новый пароль по действительному одноразовому токену.
    /// </summary>
    /// <param name="token">Исходный токен восстановления.</param>
    /// <param name="newPassword">Новый пароль.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>true, если пароль успешно изменён.</returns>
    Task<bool> ResetAsync(
        string token,
        string newPassword,
        CancellationToken cancellationToken = default);
}