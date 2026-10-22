using Authentication.Api;
using Authentication.Domain;

namespace Authentication.Abstractions;

/// <summary>Управляет жизненным циклом refresh-токенов.</summary>
public interface IRefreshTokenService
{
    /// <summary>Выдаёт новую пару токенов.</summary>
    Task<RefreshTokenResponse> IssueAsync(
        AuthUser user,
        CancellationToken cancellationToken = default);

    /// <summary>Обновляет пару токенов с отзывом предыдущего refresh-токена.</summary>
    Task<RefreshTokenResponse?> RotateAsync(
        string rawToken,
        CancellationToken cancellationToken = default);

    /// <summary>Отзывает один refresh-токен.</summary>
    Task<bool> RevokeAsync(
        string rawToken,
        CancellationToken cancellationToken = default);

    /// <summary>Отзывает все активные refresh-токены пользователя.</summary>
    Task RevokeAllAsync(
        Guid userId,
        DateTimeOffset? revokedAtUtc = null,
        CancellationToken cancellationToken = default);
}