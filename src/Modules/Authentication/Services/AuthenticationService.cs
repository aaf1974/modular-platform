using Authentication.Abstractions;
using Authentication.Api;
using Authentication.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Platform.EntityFrameworkCore;

namespace Authentication;

/// <summary>Выполняет регистрацию и аутентификацию пользователей.</summary>
internal sealed class AuthenticationService(ApplicationDbContext dbContext, 
    IPasswordHasher<AuthUser> passwordHasher, 
    IJwtTokenService tokenService,
    IRefreshTokenService refreshTokenService)
    : IAuthenticationService
{
    public async Task<bool> RegisterAsync(string email, string password, CancellationToken cancellationToken)
    {
        email = email.Trim();

        if (!IsValidEmail(email) || password.Length < 12)
            return false;

        var normalizedEmail = email.ToUpperInvariant();

        if (await dbContext.Set<AuthUser>().AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken))
            return false;

        var user = new AuthUser
        {
            Email = email,
            NormalizedEmail = normalizedEmail,
            PasswordHash = string.Empty
        };

        user.PasswordHash = passwordHasher.HashPassword(user, password);

        dbContext.Set<AuthUser>().Add(user);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
            when (IsEmailUniqueViolation(exception))
        {
            return false;
        }
    }
    
    public async Task<RefreshTokenResponse?> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();

        var user = await dbContext.Set<AuthUser>()
            .SingleOrDefaultAsync(
                item => item.NormalizedEmail == normalizedEmail,
                cancellationToken);

        if (user is null || user.IsBlocked)
            return null;

        var result = passwordHasher.VerifyHashedPassword(
            user, user.PasswordHash, password);

        if (result == PasswordVerificationResult.Failed)
            return null;

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, password);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return await refreshTokenService.IssueAsync(user, cancellationToken);
    }

    public async Task<bool> ChangePasswordAsync(Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        if (newPassword.Length is < 12 or > 128)
        {
            return false;
        }

        var user = await dbContext.Set<AuthUser>()
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);

        if (user is null || user.IsBlocked)
        {
            return false;
        }

        var verification = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            currentPassword);

        if (verification == PasswordVerificationResult.Failed)
        {
            return false;
        }

        IDbContextTransaction? transaction = null;
        try
        {
            if (dbContext.Database.IsRelational())
            {
                transaction = await dbContext.Database.BeginTransactionAsync(
                    cancellationToken);
            }

            user.PasswordHash = passwordHasher.HashPassword(user, newPassword);
            await dbContext.SaveChangesAsync(cancellationToken);
            await refreshTokenService.RevokeAllAsync(user.Id, cancellationToken: cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
            return true;
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
        // user.PasswordHash = passwordHasher.HashPassword(user, newPassword);
        // await dbContext.SaveChangesAsync(cancellationToken);
        // // После смены пароля старые refresh-токены больше не должны
        // // позволять создавать новые access-токены.
        // await refreshTokenService.RevokeAllAsync(user.Id, cancellationToken: cancellationToken);
        // return true;
    }
    
    public async Task<bool> SetBlockedAsync(Guid userId, bool isBlocked, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Set<AuthUser>()
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);

        if (user is null)
        {
            return false;
        }

        if (user.IsBlocked == isBlocked)
        {
            return true;
        }

        user.IsBlocked = isBlocked;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (isBlocked)
        {
            await refreshTokenService.RevokeAllAsync(user.Id, cancellationToken: cancellationToken);
        }

        return true;
    }
    
    static bool IsEmailUniqueViolation(Exception exception)
    {
        for (Exception? current = exception;
             current is not null;
             current = current.InnerException)
        {
            if (current is PostgresException postgresException &&
                postgresException.SqlState == PostgresErrorCodes.UniqueViolation 
                // TODO: Make It
                // && postgresException.ConstraintName == "ux_auth_users_normalized_email"
               )
            {
                return true;
            }
        }

        return false;
    }
    
    private static bool IsValidEmail(string email) =>
        email.Length <= 320 &&
        System.Net.Mail.MailAddress.TryCreate(email, out var address) &&
        string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase);
}