using Authentication.Domain;
using Microsoft.AspNetCore.Identity;

namespace Demo.Api.IntegrationTests.AuthTests;

public sealed class AuthUserPasswordTests
{
    [Fact(DisplayName = "Пароль хранится в виде хеша")]
    public void PasswordHasher_ShouldNotStorePlainText()
    {
        var user = new AuthUser
        {
            Email = "user@example.com",
            NormalizedEmail = "USER@EXAMPLE.COM",
            PasswordHash = string.Empty
        };

        const string password = "Secure-Test-Password-123!";

        var hasher = new PasswordHasher<AuthUser>();
        user.PasswordHash = hasher.HashPassword(user, password);

        user.PasswordHash.ShouldNotBe(password);

        hasher.VerifyHashedPassword(user, user.PasswordHash, password)
            .ShouldBe(PasswordVerificationResult.Success);

        hasher.VerifyHashedPassword(user, user.PasswordHash, "Wrong-Password-123!")
            .ShouldBe(PasswordVerificationResult.Failed);
    }
}