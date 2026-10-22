using Authentication.Api;

namespace Authentication.Abstractions;

/// <summary>Выполняет сценарии аутентификации и управления учётной записью.</summary>
public interface IAuthenticationService
{
    /// <summary>Регистрирует пользователя.</summary>
    Task<bool> RegisterAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>Проверяет учётные данные и выдаёт пару токенов.</summary>
    Task<RefreshTokenResponse?> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>Изменяет пароль текущего пользователя.</summary>
    Task<bool> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>Изменяет состояние блокировки пользователя.</summary>
    Task<bool> SetBlockedAsync(
        Guid userId,
        bool isBlocked,
        CancellationToken cancellationToken = default);
}