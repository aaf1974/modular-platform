using System.Net;
using System.Net.Http.Json;
using Authentication.Api;
using Authentication.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Platform.EntityFrameworkCore;
using Shouldly;

namespace Demo.Api.IntegrationTests.AuthTests;

public partial class AuthenticationModuleIntegrationTests
{
    [Fact(DisplayName = "Регистрация создаёт пользователя с хешем пароля")]
    public async Task Register_Should_Create_User_With_Hashed_Password()
    {
        var email = UniqueEmail();

        var response = await Client.PostAsJsonAsync(
            "/auth/register",
            new RegisterRequest(email, ValidPassword));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = await db.Set<AuthUser>()
            .SingleAsync(x => x.NormalizedEmail == email.ToUpperInvariant());

        user.Email.ShouldBe(email);
        user.PasswordHash.ShouldNotBe(ValidPassword);

        var hasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher<AuthUser>>();

        ShouldBeTestExtensions.ShouldBe(hasher.VerifyHashedPassword(user, user.PasswordHash, ValidPassword),
            PasswordVerificationResult.Success);

        ShouldBeTestExtensions.ShouldBe(user.Role, AuthUserRole.User);
        user.IsBlocked.ShouldBeFalse();
    }

    [Fact(DisplayName = "Повторная регистрация одного email отклоняется")]
    public async Task Register_Duplicate_Email_Should_Return_BadRequest()
    {
        var email = UniqueEmail();

        var first = await Client.PostAsJsonAsync(
            "/auth/register", new RegisterRequest(email, ValidPassword));

        var second = await Client.PostAsJsonAsync(
            "/auth/register", new RegisterRequest(email.ToUpperInvariant(), ValidPassword));

        first.StatusCode.ShouldBe(HttpStatusCode.Created);
        second.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory(DisplayName = "Регистрация отклоняет некорректные данные")]
    [InlineData("not-an-email", "Integration-Test-Password-123!")]
    [InlineData("", "Integration-Test-Password-123!")]
    [InlineData("valid@example.com", "short")]
    public async Task Register_Invalid_Data_Should_Return_BadRequest(string email, string password)
    {
        var response = await Client.PostAsJsonAsync(
            "/auth/register", new RegisterRequest(email, password));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "Параллельная регистрация одного email создаёт ровно одного пользователя")]
    public async Task Concurrent_Register_Should_Create_Only_One_User()
    {
        var email = UniqueEmail();

        var responses = await Task.WhenAll(
            Client.PostAsJsonAsync(
                "/auth/register",
                new RegisterRequest(email, ValidPassword)),
            Client.PostAsJsonAsync(
                "/auth/register",
                new RegisterRequest(email.ToUpperInvariant(), ValidPassword)));

        responses.Select(response => response.StatusCode)
            .ShouldBe([
                HttpStatusCode.Created,
                HttpStatusCode.BadRequest
            ], ignoreOrder: true);

        foreach (var response in responses)
            response.Dispose();

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var count = await db.Set<AuthUser>()
            .CountAsync(user =>
                user.NormalizedEmail == email.ToUpperInvariant());

        count.ShouldBe(1);
    }
}