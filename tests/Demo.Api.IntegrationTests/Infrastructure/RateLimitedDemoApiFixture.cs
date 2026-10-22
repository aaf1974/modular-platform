namespace Demo.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Интеграционная фикстура с уменьшенными лимитами запросов аутентификации.
/// </summary>
public sealed class RateLimitedDemoApiFixture : DemoApiFixture
{
    public RateLimitedDemoApiFixture()
        :base(new Dictionary<string, string?>
        {
            ["Authentication:RateLimiting:RegisterPermitLimit"] = "2",
            ["Authentication:RateLimiting:LoginPermitLimit"] = "2",
            ["Authentication:RateLimiting:RefreshPermitLimit"] = "2",
            ["Authentication:RateLimiting:WindowSeconds"] = "60"
        })
    {
        
    }
}

[CollectionDefinition(Name)]
public sealed class RateLimitedDemoApiCollection : ICollectionFixture<RateLimitedDemoApiFixture>
{
    public const string Name = "Rate-limited Demo API integration tests";
}