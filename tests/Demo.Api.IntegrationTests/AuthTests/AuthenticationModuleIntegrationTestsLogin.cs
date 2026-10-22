using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Authentication.Api;
using Authentication.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Platform.EntityFrameworkCore;
using Shouldly;

namespace Demo.Api.IntegrationTests.AuthTests;

public partial class AuthenticationModuleIntegrationTests
{
    [Fact(DisplayName = "Успешный вход возвращает JWT")]
    public async Task Login_Valid_Credentials_Should_Return_Token()
    {
        var email = UniqueEmail();
        await RegisterAsync(email);

        var response = await Client.PostAsJsonAsync(
            "/auth/login", new LoginRequest(email, ValidPassword));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();

        result.ShouldNotBeNull();
        result.AccessToken.ShouldNotBeNullOrWhiteSpace();
        result.RefreshToken.ShouldNotBeNullOrWhiteSpace();
        result.AccessTokenExpiresAtUtc.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
        result.RefreshTokenExpiresAtUtc.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
    }

    [Fact(DisplayName = "Вход с неверным паролем возвращает 401")]
    public async Task Login_Wrong_Password_Should_Return_Unauthorized()
    {
        var email = UniqueEmail();
        await RegisterAsync(email);

        var response = await Client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(email, "Wrong-Password-123!"));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "Логин возвращает access и refresh токены")]
    public async Task Login_WhenCredentialsAreValid_ReturnsBothTokens()
    {
        var email = $"refresh-{Guid.NewGuid():N}@example.com";
        const string password = "Strong-password-123!";

        var registerResponse = await fixture.Client.PostAsJsonAsync(
            "/auth/register",
            new RegisterRequest(email, password));

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginResponse = await fixture.Client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(email, password));

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var result = await loginResponse.Content
            .ReadFromJsonAsync<RefreshTokenResponse>();

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        Assert.True(result.AccessTokenExpiresAtUtc > DateTimeOffset.UtcNow);
        Assert.True(result.RefreshTokenExpiresAtUtc > result.AccessTokenExpiresAtUtc);
    }


    [Fact(DisplayName = "Заблокированный пользователь не может войти")]
    public async Task Login_Blocked_User_Should_Return_Unauthorized()
    {
        var email = UniqueEmail();

        await RegisterAsync(email);

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = await db.Set<AuthUser>()
                .SingleAsync(x => x.NormalizedEmail == email.ToUpperInvariant());

            user.IsBlocked = true;
            await db.SaveChangesAsync();
        }

        var response = await Client.PostAsJsonAsync(
            "/auth/login", new LoginRequest(email, ValidPassword));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
        
    [Fact(DisplayName = "Заблокированный пользователь не может использовать ранее выданный access-токен")]
    public async Task ProtectedEndpoint_WhenUserIsBlocked_RejectsExistingAccessToken()
    {
        var email = $"blocked-access-{Guid.NewGuid():N}@example.com";
        const string password = "Integration-Strong-Password-123!";

        var registerResponse = await fixture.Client.PostAsJsonAsync("/auth/register", new RegisterRequest(email, password));

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginResponse = await fixture.Client.PostAsJsonAsync("/auth/login", new LoginRequest(email, password));

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var tokens = await loginResponse.Content.ReadFromJsonAsync<RefreshTokenResponse>();

        Assert.NotNull(tokens);

        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Set<AuthUser>()
                .SingleAsync(item => item.NormalizedEmail == email.ToUpperInvariant());
            user.IsBlocked = true;
            await db.SaveChangesAsync();
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/change-password")
        {
            Content = JsonContent.Create(new ChangePasswordRequest(password, "New-Strong-Password-456!"))
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var response = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}