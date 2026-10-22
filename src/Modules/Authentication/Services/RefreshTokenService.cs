using System.Security.Cryptography;
using System.Text;
using Authentication.Abstractions;
using Authentication.Api;
using Authentication.Configuration;
using Authentication.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Platform.EntityFrameworkCore;

namespace Authentication;

/// <summary>
/// Управляет выдачей, обновлением и отзывом refresh-токенов.
/// </summary>
internal sealed class RefreshTokenService(
    ApplicationDbContext dbContext,
    IJwtTokenService jwtTokenService,
    IOptions<AuthenticationOptions> options,
    TimeProvider timeProvider) : IRefreshTokenService
{
    private readonly TimeSpan _lifetime = TimeSpan.FromDays(options.Value.RefreshTokenLifetimeDays);

    /// <summary>
    /// Создаёт refresh-токен для пользователя.
    /// </summary>
    public async Task<RefreshTokenResponse> IssueAsync(AuthUser user, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.Add(_lifetime);
        var rawToken = GenerateToken();

        dbContext.Set<RefreshToken>().Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            CreatedAtUtc = now,
            ExpiresAtUtc = expiresAt
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var accessToken = jwtTokenService.CreateToken(user);

        return new RefreshTokenResponse(
            accessToken.AccessToken,
            accessToken.ExpiresAtUtc,
            rawToken,
            expiresAt);
    }

    /// <summary>
    /// Обновляет пару токенов и отзывает использованный refresh-токен.
    /// </summary>
    /// <returns>Новая пара токенов или null, если токен недействителен.</returns>
    public async Task<RefreshTokenResponse?> RotateAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return null;
        }

        var hash = HashToken(rawToken);

        var storedToken = await dbContext.Set<RefreshToken>()
            .Include(token => token.User)
            .SingleOrDefaultAsync(
                token => token.TokenHash == hash,
                cancellationToken);

        if (storedToken is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();

        // Повторное использование отозванного токена — признак компрометации.
        if (storedToken.RevokedAtUtc is not null)
        {
            await RevokeAllAsync(storedToken.UserId, now, cancellationToken);
            return null;
        }

        if (storedToken.ExpiresAtUtc <= now || storedToken.User.IsBlocked)
        {
            storedToken.RevokedAtUtc = now;
            await dbContext.SaveChangesAsync(cancellationToken);

            if (storedToken.User.IsBlocked)
            {
                await RevokeAllAsync(storedToken.UserId, now, cancellationToken);
            }

            return null;
        }

        var newRawToken = GenerateToken();
        var newToken = new RefreshToken
        {
            UserId = storedToken.UserId,
            TokenHash = HashToken(newRawToken),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(_lifetime)
        };

        storedToken.RevokedAtUtc = now;
        storedToken.ReplacedByTokenId = newToken.Id;

        dbContext.Set<RefreshToken>().Add(newToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Другой запрос уже успел изменить этот refresh-токен.
            // Убираем устаревшие сущности и отзываем оставшиеся сессии
            // пользователя: это может быть повторное использование токена.
            dbContext.ChangeTracker.Clear();

            await RevokeAllAsync(
                storedToken.UserId,
                timeProvider.GetUtcNow(),
                cancellationToken);

            return null;
        }

        var accessToken = jwtTokenService.CreateToken(storedToken.User);

        return new RefreshTokenResponse(
            accessToken.AccessToken,
            accessToken.ExpiresAtUtc,
            newRawToken,
            newToken.ExpiresAtUtc);
    }

    /// <summary>
    /// Отзывает все refresh-токены пользователя.
    /// </summary>
    public async Task RevokeAllAsync(Guid userId, DateTimeOffset? revokedAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        var now = revokedAtUtc ?? timeProvider.GetUtcNow();

        var activeTokens = await dbContext.Set<RefreshToken>()
            .Where(token =>
                token.UserId == userId &&
                token.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RevokeAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return false;
        }

        var hash = HashToken(rawToken);

        var storedToken = await dbContext
            .Set<RefreshToken>()
            .SingleOrDefaultAsync(
                token => token.TokenHash == hash,
                cancellationToken);

        if (storedToken is null || storedToken.RevokedAtUtc is not null)
        {
            return false;
        }

        storedToken.RevokedAtUtc = timeProvider.GetUtcNow();

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            return false;
        }
    }

    private static string GenerateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string HashToken(string token) =>
        Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}