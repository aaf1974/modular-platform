using Authentication.Abstractions;
using Authentication.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Platform.EntityFrameworkCore;

namespace Platform.Core.Tests.AuthTests;

public sealed class AuthenticationServiceTests
{
    [Fact(DisplayName = "Регистрация сохраняет пользователя с хешем пароля")]
    public async Task RegisterAsync_Should_Save_Hashed_Password()
    {
        await using var provider = AuthModuleFixture.CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var result = await service.RegisterAsync(
            "user@example.com",
            "StrongPassword123!",
            CancellationToken.None);

        result.ShouldBeTrue();

        var user = await dbContext.Set<AuthUser>().SingleAsync();

        user.Email.ShouldBe("user@example.com");
        user.NormalizedEmail.ShouldBe("USER@EXAMPLE.COM");
        user.PasswordHash.ShouldNotBe("StrongPassword123!");
        user.Role.ShouldBe(AuthUserRole.User);
        user.IsBlocked.ShouldBeFalse();
    }

    [Theory(DisplayName = "Регистрация отклоняет некорректные данные")]
    [InlineData("not-an-email", "StrongPassword123!")]
    [InlineData("user@example.com", "short")]
    public async Task RegisterAsync_Should_Reject_Invalid_Input(string email, string password)
    {
        await using var provider = AuthModuleFixture.CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var result = await service.RegisterAsync(
            email,
            password,
            CancellationToken.None);

        result.ShouldBeFalse();
        (await dbContext.Set<AuthUser>().CountAsync()).ShouldBe(0);
    }

    [Fact(DisplayName = "Регистрация отклоняет повторный email без учёта регистра")]
    public async Task RegisterAsync_Should_Reject_Duplicate_Email()
    {
        await using var provider = AuthModuleFixture.CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        (await service.RegisterAsync(
            "user@example.com",
            "StrongPassword123!",
            CancellationToken.None)).ShouldBeTrue();

        var duplicate = await service.RegisterAsync(
            "USER@EXAMPLE.COM",
            "AnotherPassword123!",
            CancellationToken.None);

        duplicate.ShouldBeFalse();
        (await dbContext.Set<AuthUser>().CountAsync()).ShouldBe(1);
    }

    [Fact(DisplayName = "Вход возвращает токен при верном пароле")]
    public async Task LoginAsync_Should_Return_Token_For_Valid_Credentials()
    {
        await using var provider = AuthModuleFixture.CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
        
        await service.RegisterAsync(
            "user@example.com",
            "StrongPassword123!",
            CancellationToken.None);

        var response = await service.LoginAsync(
            "USER@example.com",
            "StrongPassword123!",
            CancellationToken.None);

        response.ShouldNotBeNull();
        response.AccessToken.ShouldNotBeNullOrWhiteSpace();
        response.RefreshTokenExpiresAtUtc.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
    }

    [Fact(DisplayName = "Вход отклоняет неверный пароль")]
    public async Task LoginAsync_Should_Reject_Wrong_Password()
    {
        await using var provider = AuthModuleFixture.CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
        
        await service.RegisterAsync(
            "user@example.com",
            "StrongPassword123!",
            CancellationToken.None);

        var response = await service.LoginAsync(
            "user@example.com",
            "WrongPassword123!",
            CancellationToken.None);

        response.ShouldBeNull();
    }

    [Fact(DisplayName = "Заблокированный пользователь не может войти")]
    public async Task LoginAsync_Should_Reject_Blocked_User()
    {
        await using var provider = AuthModuleFixture.CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        await service.RegisterAsync(
            "user@example.com",
            "StrongPassword123!",
            CancellationToken.None);

        var user = await dbContext.Set<AuthUser>().SingleAsync();
        user.IsBlocked = true;
        await dbContext.SaveChangesAsync();

        var response = await service.LoginAsync(
            "user@example.com",
            "StrongPassword123!",
            CancellationToken.None);

        response.ShouldBeNull();
    }
}