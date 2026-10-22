using System.Data;
using System.Security.Cryptography;
using System.Text;
using Authentication.Abstractions;
using Authentication.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Platform.EntityFrameworkCore;

namespace Authentication;

/// <summary>
/// Выполняет восстановление пароля по одноразовому токену.
/// </summary>
internal sealed class PasswordResetService(
    ApplicationDbContext dbContext,
    IPasswordHasher<AuthUser> passwordHasher,
    IRefreshTokenService refreshTokenService,
    IEmailSender emailSender,
    TimeProvider timeProvider,
    ILogger<PasswordResetService> logger)
    : IPasswordResetService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(30);

    /// <inheritdoc />
    public async Task RequestResetAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();

        var user = await dbContext.Set<AuthUser>()
            .SingleOrDefaultAsync(
                item => item.NormalizedEmail == normalizedEmail,
                cancellationToken);

        // Не выдаём информацию о существовании учётной записи.
        if (user is null || user.IsBlocked)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();

        // Новый запрос делает предыдущие неиспользованные токены недействительными.
        var previousTokens = await dbContext.Set<PasswordResetToken>()
            .Where(token => token.UserId == user.Id && token.UsedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var previousToken in previousTokens)
        {
            previousToken.UsedAtUtc = now;
        }

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        dbContext.Set<PasswordResetToken>().Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(TokenLifetime)
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await emailSender.SendPasswordResetAsync(
                user.Email,
                rawToken,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Не записываем сам токен в лог.
            logger.LogError(
                exception,
                "Не удалось отправить письмо для восстановления пароля.");
        }
    }

    /// <inheritdoc />
    public async Task<bool> ResetAsync(
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token) ||
            string.IsNullOrEmpty(newPassword) ||
            newPassword.Length is < 12 or > 128)
        {
            return false;
        }

        var now = timeProvider.GetUtcNow();
        var tokenHash = HashToken(token);

        var resetToken = await dbContext.Set<PasswordResetToken>()
            .Include(item => item.User)
            .SingleOrDefaultAsync(
                item => item.TokenHash == tokenHash,
                cancellationToken);

        if (resetToken is null ||
            resetToken.UsedAtUtc is not null ||
            resetToken.ExpiresAtUtc <= now ||
            resetToken.User.IsBlocked)
        {
            return false;
        }

        IDbContextTransaction? transaction = null;

        try
        {
            if (dbContext.Database.IsRelational())
            {
                transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            }

            // UsedAtUtc настроен как concurrency token: параллельная попытка
            // использовать тот же токен не должна успешно сохранить изменения.
            resetToken.UsedAtUtc = now;
            resetToken.User.PasswordHash = passwordHasher.HashPassword(
                resetToken.User,
                newPassword);

            await dbContext.SaveChangesAsync(cancellationToken);

            await refreshTokenService.RevokeAllAsync(
                resetToken.UserId,
                cancellationToken: cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            dbContext.ChangeTracker.Clear();
            return false;
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}