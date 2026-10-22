using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Authentication.Api;
using Authentication.Domain;
using Demo.Api.IntegrationTests.Infrastructure;
using Shouldly;

namespace Demo.Api.IntegrationTests.AuthTests;

[Collection(DemoApiCollection.Name)]
public sealed partial class AuthenticationModuleIntegrationTests(DemoApiFixture fixture) : IClassFixture<DemoApiFixture>
{
    private HttpClient Client => fixture.Client;

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";

    private const string ValidPassword = "Integration-Test-Password-123!";
    
    [Fact(DisplayName = "Анонимный запрос профиля возвращает 401")]
    public async Task GetMe_Without_Token_Should_Return_Unauthorized()
    {
        using var response = await Client.GetAsync("/auth/me");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "Валидный JWT позволяет получить профиль")]
    public async Task GetMe_With_Valid_Token_Should_Return_User()
    {
        var email = UniqueEmail();
        await RegisterAsync(email);

        var loginResponse = await Client.PostAsJsonAsync(
            "/auth/login", new LoginRequest(email, ValidPassword));

        loginResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        login.ShouldNotBeNull();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/me");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", login.AccessToken);

        using var response = await Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var user = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();
        user.ShouldNotBeNull();
        user.Email.ShouldBe(email);
        user.Role.ShouldBe(nameof(AuthUserRole.User));
    }

    [Fact(DisplayName = "JWT с неверной подписью отклоняется")]
    public async Task GetMe_With_Invalid_Token_Should_Return_Unauthorized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/me");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", "invalid.token.signature");

        using var response = await Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "Refresh ротирует токен и отзывает предыдущий")]
    public async Task Refresh_WhenTokenIsValid_ReturnsNewPairAndRevokesOldToken()
    {
        var tokens = await RegisterAndLoginAsync();

        var refreshResponse = await fixture.Client.PostAsJsonAsync(
            "/auth/refresh",
            new RefreshTokenRequest(tokens.RefreshToken));

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var rotated = await refreshResponse.Content
            .ReadFromJsonAsync<RefreshTokenResponse>();

        Assert.NotNull(rotated);
        Assert.NotEqual(tokens.RefreshToken, rotated.RefreshToken);
        Assert.NotEqual(tokens.AccessToken, rotated.AccessToken);

        var replayResponse = await fixture.Client.PostAsJsonAsync(
            "/auth/refresh",
            new RefreshTokenRequest(tokens.RefreshToken));

        Assert.Equal(HttpStatusCode.Unauthorized, replayResponse.StatusCode);

        // Повторное использование старого токена отзывает цепочку сессии.
        var rotatedTokenResponse = await fixture.Client.PostAsJsonAsync(
            "/auth/refresh",
            new RefreshTokenRequest(rotated.RefreshToken));

        Assert.Equal(HttpStatusCode.Unauthorized, rotatedTokenResponse.StatusCode);
    }
    
    [Fact(DisplayName = "Logout отзывает refresh-токен")]
    public async Task Logout_WhenTokenIsValid_RevokesSession()
    {
        var tokens = await RegisterAndLoginAsync();

        var logoutResponse = await fixture.Client.PostAsJsonAsync(
            "/auth/logout",
            new LogoutRequest(tokens.RefreshToken));

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var refreshResponse = await fixture.Client.PostAsJsonAsync(
            "/auth/refresh",
            new RefreshTokenRequest(tokens.RefreshToken));

        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }
    
    [Fact(DisplayName = "Refresh отклоняет неизвестный токен")]
    public async Task Refresh_WhenTokenIsUnknown_ReturnsUnauthorized()
    {
        var response = await fixture.Client.PostAsJsonAsync(
            "/auth/refresh",
            new RefreshTokenRequest("unknown-refresh-token"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    
    [Fact(DisplayName = "Параллельная ротация не позволяет дважды использовать один токен")]
    public async Task Refresh_WhenRequestsAreConcurrent_OnlyOneRotationSucceeds()
    {
        var tokens = await RegisterAndLoginAsync();

        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        firstRequest.Content = JsonContent.Create(new RefreshTokenRequest(tokens.RefreshToken));

        using var secondRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        secondRequest.Content = JsonContent.Create(new RefreshTokenRequest(tokens.RefreshToken));

        var responses = await Task.WhenAll(
            fixture.Client.SendAsync(firstRequest),
            fixture.Client.SendAsync(secondRequest));

        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.OK));

        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Unauthorized));
    }
    
   
    private async Task RegisterAsync(string email)
    {
        var response = await Client.PostAsJsonAsync(
            "/auth/register", new RegisterRequest(email, ValidPassword));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }
    
    private async Task<RefreshTokenResponse> RegisterAndLoginAsync()
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

        var tokens = await loginResponse.Content
            .ReadFromJsonAsync<RefreshTokenResponse>();

        Assert.NotNull(tokens);
        return tokens;
    }
}