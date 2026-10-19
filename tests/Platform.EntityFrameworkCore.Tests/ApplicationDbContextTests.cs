using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Core.Modules;
using Platform.EntityFrameworkCore.TestFixtures;

namespace Platform.EntityFrameworkCore.Tests;

public sealed class ApplicationDbContextTests
{
    [Fact(DisplayName = "ApplicationDbContext применяет конфигурации EF Core зарегистрированных модулей")]
    public void OnModelCreating_WhenModuleProvidesConfigurationAssembly_AppliesConfiguration()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddModule<SampleEfModule>(configuration, "Test");
        services.AddPlatformEntityFrameworkCore(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        using var serviceProvider = services.BuildServiceProvider();
        using var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

        var entityType = context.Model.FindEntityType(typeof(ModuleEntity));

        Assert.NotNull(entityType);
        Assert.Equal("module_entities", entityType!.GetTableName());
    }

    [Fact(DisplayName = "ApplicationDbContext допускает отсутствие модулей с EF-конфигурациями")]
    public void OnModelCreating_WhenNoEfModulesRegistered_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddPlatformEntityFrameworkCore(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        using var serviceProvider = services.BuildServiceProvider();
        using var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.NotNull(context.Model);
        Assert.Null(context.Model.FindEntityType(typeof(ModuleEntity)));
    }

    [Fact(DisplayName = "Фабрика должна создавать ApplicationDbContext с конфигурациями зарегистрированных модулей")]
    public async Task AddPlatformEntityFrameworkCore_WhenResolvedFactory_CreatesConfiguredContext()
    {
        var services = new ServiceCollection();
        services.AddModule<SampleEfModule>(new ConfigurationBuilder().Build(), "Test");
        services.AddPlatformEntityFrameworkCore(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        using var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();

        await using var context = await factory.CreateDbContextAsync();

        Assert.NotNull(context.Model.FindEntityType(typeof(ModuleEntity)));
    }

    [Fact(DisplayName = "ApplicationDbContext остаётся доступен как scoped-сервис при регистрации фабрики")]
    public void AddPlatformEntityFrameworkCore_WhenResolvedDirectly_ProvidesApplicationDbContext()
    {
        var services = new ServiceCollection();
        services.AddPlatformEntityFrameworkCore(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.NotNull(context);
    }
}
