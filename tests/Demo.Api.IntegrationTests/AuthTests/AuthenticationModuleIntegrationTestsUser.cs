using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Authentication.Api;
using Authentication.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Platform.EntityFrameworkCore;

namespace Demo.Api.IntegrationTests.AuthTests;

public partial class AuthenticationModuleIntegrationTests
{
    [Fact(DisplayName = "Смена пароля отзывает refresh-токены и отключает старый пароль")]
    public async Task ChangePassword_WhenCurrentPasswordIsValid_ChangesPasswordAndRevokesTokens()
    {
        var email = $"change-password-{Guid.NewGuid():N}@example.com";
        const string oldPassword = "Old-Strong-Password-123!";
        const string newPassword = "New-Strong-Password-456!";

        var register = await fixture.Client.PostAsJsonAsync(
            "/auth/register",
            new RegisterRequest(email, oldPassword));

        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await fixture.Client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(email, oldPassword));

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var tokens = await login.Content.ReadFromJsonAsync<RefreshTokenResponse>();
        Assert.NotNull(tokens);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/change-password");
        request.Content = JsonContent.Create(new ChangePasswordRequest(oldPassword, newPassword));

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var changeResponse = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, changeResponse.StatusCode);

        var oldLogin = await fixture.Client.PostAsJsonAsync("/auth/login", new LoginRequest(email, oldPassword));

        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

        var oldRefresh =
            await fixture.Client.PostAsJsonAsync("/auth/refresh", new RefreshTokenRequest(tokens.RefreshToken));

        Assert.Equal(HttpStatusCode.Unauthorized, oldRefresh.StatusCode);

        var newLogin = await fixture.Client.PostAsJsonAsync("/auth/login", new LoginRequest(email, newPassword));

        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }

    [Fact(DisplayName = "Смена пароля с неверным текущим паролем отклоняется")]
    public async Task ChangePassword_WhenCurrentPasswordIsInvalid_ReturnsBadRequest()
    {
        var email = $"change-password-{Guid.NewGuid():N}@example.com";
        const string password = "Original-Strong-Password-123!";

        var register = await fixture.Client.PostAsJsonAsync(
            "/auth/register",
            new RegisterRequest(email, password));

        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await fixture.Client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(email, password));

        var tokens = await login.Content.ReadFromJsonAsync<RefreshTokenResponse>();
        Assert.NotNull(tokens);

        using var request = new HttpRequestMessage(
            HttpMethod.Post, "/auth/change-password")
        {
            Content = JsonContent.Create(
                new ChangePasswordRequest(
                    "Wrong-Password-123!",
                    "Replacement-Password-456!"))
        };

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var oldPasswordLogin = await fixture.Client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(email, password));

        Assert.Equal(HttpStatusCode.OK, oldPasswordLogin.StatusCode);
    }

    [Fact(DisplayName = "Администратор блокирует пользователя и отзывает его refresh-токен")]
    public async Task BlockUser_WhenCalledByAdministrator_BlocksUserAndRevokesTokens()
    {
        var adminEmail = $"admin-{Guid.NewGuid():N}@example.com";
        var userEmail = $"blocked-{Guid.NewGuid():N}@example.com";
        const string password = "Integration-Strong-Password-123!";

        var adminRegister = await fixture.Client.PostAsJsonAsync(
            "/auth/register",
            new RegisterRequest(adminEmail, password));

        Assert.Equal(HttpStatusCode.Created, adminRegister.StatusCode);

        var userRegister = await fixture.Client.PostAsJsonAsync(
            "/auth/register",
            new RegisterRequest(userEmail, password));

        Assert.Equal(HttpStatusCode.Created, userRegister.StatusCode);

        Guid userId;

        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var admin = await db.Set<AuthUser>()
                .SingleAsync(user => user.NormalizedEmail == adminEmail.ToUpperInvariant());

            admin.Role = AuthUserRole.Administrator;

            var user = await db.Set<AuthUser>()
                .SingleAsync(item => item.NormalizedEmail == userEmail.ToUpperInvariant());

            userId = user.Id;

            await db.SaveChangesAsync();
        }

        var adminLogin = await fixture.Client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(adminEmail, password));

        var adminTokens = await adminLogin.Content
            .ReadFromJsonAsync<RefreshTokenResponse>();

        Assert.NotNull(adminTokens);

        var userLogin = await fixture.Client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(userEmail, password));

        var userTokens = await userLogin.Content
            .ReadFromJsonAsync<RefreshTokenResponse>();

        Assert.NotNull(userTokens);

        using var blockRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/auth/users/{userId}/block");

        blockRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", adminTokens.AccessToken);

        var blockResponse = await fixture.Client.SendAsync(blockRequest);

        Assert.Equal(HttpStatusCode.NoContent, blockResponse.StatusCode);

        var userLoginAfterBlock = await fixture.Client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(userEmail, password));

        Assert.Equal(HttpStatusCode.Unauthorized, userLoginAfterBlock.StatusCode);

        var refreshAfterBlock = await fixture.Client.PostAsJsonAsync(
            "/auth/refresh",
            new RefreshTokenRequest(userTokens.RefreshToken));

        Assert.Equal(HttpStatusCode.Unauthorized, refreshAfterBlock.StatusCode);
    }

    [Fact(DisplayName = "Обычный пользователь не может блокировать других пользователей")]
    public async Task BlockUser_WhenCalledByRegularUser_ReturnsForbidden()
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";
        const string password = "Integration-Strong-Password-123!";

        var register = await fixture.Client.PostAsJsonAsync(
            "/auth/register",
            new RegisterRequest(email, password));

        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await fixture.Client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(email, password));

        var tokens = await login.Content.ReadFromJsonAsync<RefreshTokenResponse>();
        Assert.NotNull(tokens);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/auth/users/{Guid.NewGuid()}/block");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}