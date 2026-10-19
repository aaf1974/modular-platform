using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Core.Modules;
using Platform.EntityFrameworkCore.Auditing;
using Platform.EntityFrameworkCore.Repositories;
using Platform.EntityFrameworkCore.TestFixtures;

namespace Platform.EntityFrameworkCore.IntegrationTests;

[Collection(PostgreSqlCollectionNames.Name)]
public sealed class ApplicationDbContextPostgreSqlTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact(DisplayName = "ApplicationDbContext сохраняет и читает данные в PostgreSQL через фабрику")]
    public async Task Factory_WhenUsedWithPostgreSql_CanPersistAndReadEntity()
    {
        var services = new ServiceCollection();
        services.AddModule<SampleEfModule>(new ConfigurationBuilder().Build(), "IntegrationTest");
        services.AddSingleton<ICurrentUser>(new TestCurrentUser("integration-user"));
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

            var repository = new EfRepository<ModuleEntity, int>(context);
            var createdThroughRepository = new ModuleEntity { Id = 2, Name = "Repository" };
            repository.Add(createdThroughRepository);
            Assert.Equal(EntityState.Added, context.Entry(createdThroughRepository).State);
            await context.SaveChangesAsync();
            Assert.Equal("Repository", (await repository.GetByIdAsync(2))!.Name);
            Assert.Contains(await repository.ListAsync(), item => item.Id == 2);

            var audited = new AuditedModuleEntity { Id = 10, Name = "Audited" };
            context.Add(audited);
            await context.SaveChangesAsync();
            Assert.Equal("integration-user", audited.CreatedByUserId);
            Assert.Null(audited.ModifiedAtUtc);
            //Assert.Equal(DateTimeKind.Utc, audited.CreatedAtUtc.Kind);
        }

        await using (var context = await factory.CreateDbContextAsync())
        {
            var audited = await context.Set<AuditedModuleEntity>().SingleAsync();
            audited.Name = "Updated";
            await context.SaveChangesAsync();
            Assert.Equal("integration-user", audited.ModifiedByUserId);
            Assert.NotNull(audited.ModifiedAtUtc);

            context.Remove(audited);
            await context.SaveChangesAsync();
            Assert.True(audited.IsDeleted);
            Assert.NotNull(audited.DeletedAtUtc);
            Assert.Equal("integration-user", audited.DeletedByUserId);
        }

        await using (var context = await factory.CreateDbContextAsync())
        {
            Assert.Empty(await context.Set<AuditedModuleEntity>().ToListAsync());
            Assert.Single(await context.Set<AuditedModuleEntity>().IgnoreQueryFilters().ToListAsync());
        }
    }

    private sealed class TestCurrentUser(string userId) : ICurrentUser
    {
        public string? UserId => userId;
    }
}