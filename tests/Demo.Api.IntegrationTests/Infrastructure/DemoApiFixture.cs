using Authentication;
using Authentication.Abstractions;
using Authentication.Configuration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Platform.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Demo.Api.IntegrationTests.Infrastructure;

public class DemoApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("demo_api_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    private WebApplicationFactory<Program>? _factory;

    public HttpClient Client { get; private set; } = null!;

    public IServiceProvider Services => _factory?.Services ?? throw new InvalidOperationException("The test application has not been initialized.");

    public WebApplicationFactory<Program> Factory => _factory ?? throw new InvalidOperationException("Fixture not initialized");

    private readonly IReadOnlyDictionary<string, string?> _configurationOverrides;
    
    public DemoApiFixture()
        : this(new Dictionary<string, string?>())
    {
    }

    protected DemoApiFixture(IReadOnlyDictionary<string, string?> configurationOverrides)
    {
        _configurationOverrides = configurationOverrides;
    }
    
    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString()
                    };

                    foreach (var (key, value) in _configurationOverrides)
                    {
                        settings[key] = value;
                    }

                    configuration.AddInMemoryCollection(settings);
                });
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IEmailSender>();
                    services.AddSingleton(new TestEmailSender());
                    services.AddSingleton<IEmailSender>(provider => provider.GetRequiredService<TestEmailSender>());
                });
                builder.ConfigureTestServices(ConfigureTestServices);
            });

        Client = _factory.CreateClient();

        var configuration = Services.GetRequiredService<IConfiguration>();
        Console.WriteLine($"[Host] LoginPermitLimit = " + configuration[$"{AuthenticationRateLimitOptions.SectionName}:LoginPermitLimit"]);

        await using var scope = Services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.MigrateAsync();
    }

    /// <summary>
    /// Настраивает зависимости, используемые в интеграционных тестах.
    /// </summary>
    /// <param name="services">Коллекция сервисов тестового приложения.</param>
    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
    }
    
    public async Task DisposeAsync()
    {
        Client?.Dispose();

        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _postgres.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class DemoApiCollection : ICollectionFixture<DemoApiFixture>
{
    public const string Name = "Demo API integration tests";
}
