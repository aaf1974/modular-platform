using Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Core.Modules;
using Platform.EntityFrameworkCore;

namespace Platform.Core.Tests.AuthTests;

public static class AuthModuleFixture
{
    internal static ServiceProvider CreateProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Issuer"] = "Authentication.Tests",
                ["Authentication:Audience"] = "Authentication.Tests",
                ["Authentication:SigningKey"] =
                    "test-signing-key-at-least-32-characters-long",
                ["Authentication:TokenLifetimeMinutes"] = "15",
                ["Authentication:RefreshTokenLifetimeDays"] = "14"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddModule<AuthenticationModule>(configuration, "Test");

        services.AddPlatformEntityFrameworkCore(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        services.AddSingleton<TimeProvider>(TimeProvider.System);

        return services.BuildServiceProvider();
    }
}