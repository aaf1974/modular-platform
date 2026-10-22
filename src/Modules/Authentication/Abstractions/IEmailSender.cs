namespace Authentication.Abstractions;

/// <summary>
/// Отправляет сообщения электронной почты.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Отправляет письмо для восстановления пароля.
    /// </summary>
    /// <param name="email">Адрес получателя.</param>
    /// <param name="resetToken">Одноразовый токен восстановления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    Task SendPasswordResetAsync(
        string email,
        string resetToken,
        CancellationToken cancellationToken = default);
}