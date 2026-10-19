using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Core.Modules;
using Platform.EntityFrameworkCore;
using Platform.EntityFrameworkCore.TestFixtures;

namespace Platform.EntityFrameworkCore.IntegrationTests;

[Collection(PostgreSqlCollectionNames.Name)]
public sealed class ApplicationDbContextPostgreSqlTests(PostgreSqlFixture fixture)
{
    [Fact(DisplayName = "ApplicationDbContext сохраняет и читает данные в PostgreSQL через фабрику") ]
    public async Task Factory_WhenUsedWithPostgreSql_CanPersistAndReadEntity()
    {
        var services = new ServiceCollection();
        services.AddModule<SampleEfModule>(new ConfigurationBuilder().Build(), "IntegrationTest");
        services.AddPlatformEntityFrameworkCore(options => options.UseNpgsql(fixture.ConnectionString));

        await using var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();

        await using (var context = await factory.CreateDbContextAsync())
        {
            await context.Database.EnsureCreatedAsync();
            context.Set<ModuleEntity>().Add(new ModuleEntity { Id = 1, Name = "PostgreSQL" });
            await context.SaveChangesAsync();
        }

        await using (var context = await factory.CreateDbContextAsync())
        {
            var entity = await context.Set<ModuleEntity>().SingleAsync();
            Assert.Equal(1, entity.Id);
            Assert.Equal("PostgreSQL", entity.Name);
            Assert.Equal("module_entities", context.Model.FindEntityType(typeof(ModuleEntity))!.GetTableName());
        }
    }
}
