using System.Net;
using System.Net.Http.Json;
using Authentication.Api;
using Authentication.Configuration;
using Demo.Api.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Demo.Api.IntegrationTests.AuthTests;

[Collection(RateLimitedDemoApiCollection.Name)]
public sealed class AuthenticationRateLimitTests(RateLimitedDemoApiFixture fixture)
{
    [Fact(DisplayName = "Превышение лимита входа возвращает HTTP 429")]
    public async Task Login_WhenLimitExceeded_ReturnsTooManyRequests()
    {
        var options = fixture.Services.GetRequiredService<IOptions<AuthenticationRateLimitOptions>>().Value;
        Console.WriteLine($"[Options] LoginPermitLimit = {options.LoginPermitLimit}");
        
        var firstResponse = await fixture.Client.PostAsJsonAsync("/auth/login",
            new LoginRequest($"unknown-{Guid.NewGuid():N}@example.com", "Wrong-Password-123!"));

        var secondResponse = await fixture.Client.PostAsJsonAsync("/auth/login",
            new LoginRequest($"unknown-{Guid.NewGuid():N}@example.com", "Wrong-Password-123!"));

        var thirdResponse = await fixture.Client.PostAsJsonAsync("/auth/login",
            new LoginRequest($"unknown-{Guid.NewGuid():N}@example.com", "Wrong-Password-123!"));

        
        Assert.Equal(HttpStatusCode.Unauthorized, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, secondResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, thirdResponse.StatusCode);
    }
}