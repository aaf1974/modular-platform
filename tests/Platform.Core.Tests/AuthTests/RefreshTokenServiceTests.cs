using Authentication.Abstractions;
using Authentication.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Platform.EntityFrameworkCore;

namespace Platform.Core.Tests.AuthTests;

public sealed class RefreshTokenServiceTests
{


    [Fact(DisplayName = "Выдача refresh-токена сохраняет только его хеш")]
    public async Task IssueAsync_Should_Store_Hash_Only()
    {
        await using var provider = AuthModuleFixture.CreateProvider();
        await using var scope = provider.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>();

        var user = await CreateUserAsync(db);

        var result = await service.IssueAsync(user);

        result.RefreshToken.ShouldNotBeNullOrWhiteSpace();
        result.AccessToken.ShouldNotBeNullOrWhiteSpace();
        result.RefreshToken.ShouldNotBe(result.AccessToken);

        var stored = await db.Set<RefreshToken>().SingleAsync();

        stored.TokenHash.ShouldNotBe(result.RefreshToken);
        stored.TokenHash.Length.ShouldBe(64);
        stored.RevokedAtUtc.ShouldBeNull();
        stored.ExpiresAtUtc.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
    }

    [Fact(DisplayName = "Обновление заменяет refresh-токен новым")]
    public async Task RotateAsync_Should_Replace_Refresh_Token()
    {
        await using var provider = AuthModuleFixture.CreateProvider();
        await using var scope = provider.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>();

        var user = await CreateUserAsync(db);
        var original = await service.IssueAsync(user);

        var rotated = await service.RotateAsync(original.RefreshToken);

        rotated.ShouldNotBeNull();
        rotated.RefreshToken.ShouldNotBe(original.RefreshToken);
        rotated.AccessToken.ShouldNotBeNullOrWhiteSpace();

        var tokens = await db.Set<RefreshToken>()
            .OrderBy(token => token.CreatedAtUtc)
            .ToListAsync();

        tokens.Count.ShouldBe(2);
        tokens[0].RevokedAtUtc.ShouldNotBeNull();
        tokens[0].ReplacedByTokenId.ShouldBe(tokens[1].Id);
        tokens[1].RevokedAtUtc.ShouldBeNull();
    }

    [Fact(DisplayName = "Повторное использование старого токена отзывает активные токены пользователя")]
    public async Task RotateAsync_Should_Revoke_Session_On_Token_Reuse()
    {
        await using var provider = AuthModuleFixture.CreateProvider();
        await using var scope = provider.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>();

        var user = await CreateUserAsync(db);
        var original = await service.IssueAsync(user);

        var rotated = await service.RotateAsync(original.RefreshToken);
        rotated.ShouldNotBeNull();

        var replay = await service.RotateAsync(original.RefreshToken);

        replay.ShouldBeNull();

        var tokens = await db.Set<RefreshToken>()
            .Where(token => token.UserId == user.Id)
            .ToListAsync();

        tokens.ShouldNotBeEmpty();
        tokens.ShouldAllBe(token => token.RevokedAtUtc != null);
    }

    [Fact(DisplayName = "Неизвестный refresh-токен отклоняется")]
    public async Task RotateAsync_Should_Reject_Unknown_Token()
    {
        await using var provider = AuthModuleFixture.CreateProvider();
        await using var scope = provider.CreateAsyncScope();

        var service = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>();

        var result = await service.RotateAsync("unknown-token");

        result.ShouldBeNull();
    }

    [Fact(DisplayName = "Заблокированный пользователь не может обновить токен")]
    public async Task RotateAsync_Should_Reject_Blocked_User()
    {
        await using var provider = AuthModuleFixture.CreateProvider();
        await using var scope = provider.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>();

        var user = await CreateUserAsync(db);
        var original = await service.IssueAsync(user);

        user.IsBlocked = true;
        await db.SaveChangesAsync();

        var result = await service.RotateAsync(original.RefreshToken);

        result.ShouldBeNull();

        var tokens = await db.Set<RefreshToken>()
            .Where(token => token.UserId == user.Id)
            .ToListAsync();

        tokens.ShouldAllBe(token => token.RevokedAtUtc != null);
    }

    private static async Task<AuthUser> CreateUserAsync(ApplicationDbContext db)
    {
        var user = new AuthUser
        {
            Email = $"{Guid.NewGuid():N}@example.com",
            NormalizedEmail = $"{Guid.NewGuid():N}@example.com".ToUpperInvariant(),
            PasswordHash = "test-password-hash"
        };

        // Нормализованное значение должно соответствовать Email.
        user.NormalizedEmail = user.Email.ToUpperInvariant();

        db.Set<AuthUser>().Add(user);
        await db.SaveChangesAsync();

        return user;
    }
}