using System.Net;
using System.Net.Http.Json;
using Authentication;
using Authentication.Abstractions;
using Authentication.Api;
using Demo.Api.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Demo.Api.IntegrationTests.AuthTests;

[Collection(DemoApiCollection.Name)]
public class AuthenticationForgotPassword(DemoApiFixture fixture) : IClassFixture<DemoApiFixture>
{
    private HttpClient Client => fixture.Client;
    
    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";
    
    private const string ValidPassword = "Integration-Test-Password-123!";
    
    [Fact(DisplayName = "Запрос восстановления пароля неизвестного email не раскрывает существование пользователя")]
    public async Task ForgotPassword_UnknownEmail_Should_Return_Ok()
    {
        using var response = await Client.PostAsJsonAsync(
            "/auth/forgot-password",
            new ForgotPasswordRequest(UniqueEmail()));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var emailSender = fixture.Services.GetRequiredService<IEmailSender>();
        ((TestEmailSender)emailSender).Messages.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Запрос восстановления пароля отправляет письмо зарегистрированному пользователю")]
    public async Task ForgotPassword_ExistingUser_Should_Send_Email()
    {
        var email = UniqueEmail();

        using var registerResponse = await Client.PostAsJsonAsync(
            "/auth/register",
            new { Email = email, Password = ValidPassword });

        registerResponse.StatusCode.ShouldBeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Created);

        using var response = await Client.PostAsJsonAsync(
            "/auth/forgot-password",
            new ForgotPasswordRequest(email));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var emailSender = fixture.Services.GetRequiredService<IEmailSender>();
        ((TestEmailSender)emailSender).Messages
            .ShouldContain(message => message.Email == email);
    }
    
    [Fact(DisplayName = "Токен восстановления позволяет установить новый пароль только один раз")]
    public async Task ResetPassword_Token_Should_Be_Usable_Only_Once()
    {
        var email = UniqueEmail();
        const string newPassword = "New-Integration-Password-456!";

        using var registerResponse = await Client.PostAsJsonAsync(
            "/auth/register",
            new { Email = email, Password = ValidPassword });

        registerResponse.StatusCode.ShouldBeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Created);

        using var forgotResponse = await Client.PostAsJsonAsync(
            "/auth/forgot-password",
            new ForgotPasswordRequest(email));

        forgotResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var emailSender = fixture.Services.GetRequiredService<IEmailSender>();
        
        var message = ((TestEmailSender)emailSender).Messages
            .Last(item => item.Email == email);

        using var resetResponse = await Client.PostAsJsonAsync(
            "/auth/reset-password",
            new ResetPasswordRequest(newPassword, message.Token));

        resetResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var repeatedResponse = await Client.PostAsJsonAsync(
            "/auth/reset-password",
            new ResetPasswordRequest(newPassword, message.Token));

        repeatedResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}