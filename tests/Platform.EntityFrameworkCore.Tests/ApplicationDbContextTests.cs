using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Platform.Core.Modules;
using Platform.EntityFrameworkCore.TestFixtures;
using Platform.EntityFrameworkCore.Auditing;
using Platform.EntityFrameworkCore.Repositories;
using Platform.EntityFrameworkCore.Services;

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

    [Fact(DisplayName = "ApplicationDbContext сохраняет пользовательский query filter вместе с soft-delete filter")]
    public async Task QueryFilters_WhenQueryingSoftDeletableEntity_AppliesBothFilters()
    {
        var services = new ServiceCollection();
        services.AddModule<SampleEfModule>(new ConfigurationBuilder().Build(), "Test");
        services.AddPlatformEntityFrameworkCore(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        context.Set<AuditedModuleEntity>().AddRange(
            new AuditedModuleEntity { Id = 1, Name = "visible" },
            new AuditedModuleEntity { Id = 2, Name = "filtered" },
            new AuditedModuleEntity { Id = 3, Name = "deleted", IsDeleted = true });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var results = await context.Set<AuditedModuleEntity>()
            .OrderBy(entity => entity.Id)
            .Select(entity => entity.Id)
            .ToListAsync();

        Assert.Equal(new[] { 1 }, results);
    }

    [Fact(DisplayName = "IgnoreQueryFilters отключает и пользовательский, и soft-delete filters")]
    public async Task QueryFilters_WhenIgnored_DisablesAllGlobalFilters()
    {
        var services = new ServiceCollection();
        services.AddModule<SampleEfModule>(new ConfigurationBuilder().Build(), "Test");
        services.AddPlatformEntityFrameworkCore(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        context.Set<AuditedModuleEntity>().AddRange(
            new AuditedModuleEntity { Id = 1, Name = "visible" },
            new AuditedModuleEntity { Id = 2, Name = "filtered" },
            new AuditedModuleEntity { Id = 3, Name = "deleted", IsDeleted = true });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var results = await context.Set<AuditedModuleEntity>()
            .IgnoreQueryFilters()
            .OrderBy(entity => entity.Id)
            .Select(entity => entity.Id)
            .ToListAsync();

        Assert.Equal(new[] { 1, 2, 3 }, results);
    }

    [Fact(DisplayName = "Remove для ISoftDeletable сохраняет запись с audit-полями удаления")]
    public async Task SaveChangesAsync_WhenRemovingSoftDeletableEntity_SetsDeletionAuditAndKeepsRow()
    {
        var services = new ServiceCollection();
        services.AddModule<SampleEfModule>(new ConfigurationBuilder().Build(), "Test");
        services.AddPlatformEntityFrameworkCore(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var entity = new AuditedModuleEntity { Id = 10, Name = "to-delete" };

        context.Add(entity);
        await context.SaveChangesAsync();
        var createdAt = entity.CreatedAtUtc;

        context.Remove(entity);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var stored = await context.Set<AuditedModuleEntity>()
            .IgnoreQueryFilters()
            .SingleAsync(item => item.Id == 10);

        Assert.True(stored.IsDeleted);
        Assert.NotNull(stored.DeletedAtUtc);
        Assert.Equal(createdAt, stored.CreatedAtUtc);
        Assert.Null(await context.Set<AuditedModuleEntity>().SingleOrDefaultAsync(item => item.Id == 10));
    }

    [Fact(DisplayName = "Платформа регистрирует StubCurrentUser по умолчанию")]
    public void AddPlatformEntityFrameworkCore_WhenNoCurrentUserRegistered_UsesStub()
    {
        var services = new ServiceCollection();
        services.AddPlatformEntityFrameworkCore(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        using var serviceProvider = services.BuildServiceProvider();
        var currentUser = serviceProvider.GetRequiredService<ICurrentUser>();

        Assert.IsType<StubCurrentUser>(currentUser);
        Assert.Null(currentUser.UserId);
    }

    [Fact(DisplayName = "Платформа регистрирует репозиторий и CRUD-сервис по умолчанию")]
    public void AddPlatformEntityFrameworkCore_WhenResolvedGenericServices_ProvidesDefaults()
    {
        var services = new ServiceCollection();
        services.AddModule<SampleEfModule>(new ConfigurationBuilder().Build(), "Test");
        services.AddPlatformEntityFrameworkCore(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        Assert.IsType<EfRepository<ModuleEntity, int>>(
            scope.ServiceProvider.GetRequiredService<IRepository<ModuleEntity, int>>());
        Assert.IsType<EfRepository<ModuleEntity, int>>(
            scope.ServiceProvider.GetRequiredService<IEfRepository<ModuleEntity, int>>());
        Assert.IsType<CrudService<ModuleEntity, int>>(
            scope.ServiceProvider.GetRequiredService<ICrudService<ModuleEntity, int>>());
    }

    [Theory(DisplayName = "Закрытая регистрация репозитория имеет приоритет над стандартной открытой регистрацией")]
    [InlineData(true)]
    [InlineData(false)]
    public void AddPlatformEntityFrameworkCore_WhenCustomClosedRepositoryRegistered_UsesCustomRepository(
        bool registerCustomBeforePlatform)
    {
        var services = new ServiceCollection();
        services.AddModule<SampleEfModule>(new ConfigurationBuilder().Build(), "Test");

        if (registerCustomBeforePlatform)
        {
            services.AddScoped<IRepository<ModuleEntity, int>, CustomModuleEntityRepository>();
        }

        services.AddPlatformEntityFrameworkCore(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        if (!registerCustomBeforePlatform)
        {
            services.AddScoped<IRepository<ModuleEntity, int>, CustomModuleEntityRepository>();
        }

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<IRepository<ModuleEntity, int>>();
        var crudService = scope.ServiceProvider.GetRequiredService<ICrudService<ModuleEntity, int>>();

        Assert.IsType<CustomModuleEntityRepository>(repository);
        var entity = new ModuleEntity { Id = 901, Name = "Custom repository" };
        Assert.Throws<CustomRepositoryCalledException>(() => crudService.Create(entity));
    }

    [Theory(DisplayName = "Пользовательская открытая generic-регистрация репозитория сохраняет приоритет")]
    [InlineData(true)]
    [InlineData(false)]
    public void AddPlatformEntityFrameworkCore_WhenCustomOpenGenericRepositoryRegistered_UsesCustomRepository(
        bool registerCustomBeforePlatform)
    {
        var services = new ServiceCollection();
        services.AddModule<SampleEfModule>(new ConfigurationBuilder().Build(), "Test");

        if (registerCustomBeforePlatform)
        {
            services.AddScoped(typeof(IRepository<,>), typeof(CustomOpenGenericRepository<,>));
        }

        services.AddPlatformEntityFrameworkCore(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        if (!registerCustomBeforePlatform)
        {
            services.AddScoped(typeof(IRepository<,>), typeof(CustomOpenGenericRepository<,>));
        }

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        Assert.IsType<CustomOpenGenericRepository<ModuleEntity, int>>(
            scope.ServiceProvider.GetRequiredService<IRepository<ModuleEntity, int>>());
    }

    [Fact(DisplayName = "Замена IRepository не заменяет отдельный контракт IEfRepository")]
    public void AddPlatformEntityFrameworkCore_WhenOnlyRepositoryIsCustomized_LeavesEfRepositoryDefault()
    {
        var services = new ServiceCollection();
        services.AddModule<SampleEfModule>(new ConfigurationBuilder().Build(), "Test");
        services.AddScoped<IRepository<ModuleEntity, int>, CustomModuleEntityRepository>();
        services.AddPlatformEntityFrameworkCore(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        Assert.IsType<CustomModuleEntityRepository>(
            scope.ServiceProvider.GetRequiredService<IRepository<ModuleEntity, int>>());
        Assert.IsType<EfRepository<ModuleEntity, int>>(
            scope.ServiceProvider.GetRequiredService<IEfRepository<ModuleEntity, int>>());
    }

    [Fact(DisplayName = "CRUD-сервис не сохраняет изменения автоматически")]
    public async Task CrudService_WhenCreatingEntity_LeavesSaveChangesToCaller()
    {
        var services = new ServiceCollection();
        services.AddModule<SampleEfModule>(new ConfigurationBuilder().Build(), "Test");
        services.AddPlatformEntityFrameworkCore(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<ICrudService<ModuleEntity, int>>();
        var entity = new ModuleEntity { Id = 42, Name = "Pending" };

        Assert.Same(entity, service.Create(entity));
        Assert.Equal(Microsoft.EntityFrameworkCore.EntityState.Added, context.Entry(entity).State);
        Assert.Equal(0, await context.Set<ModuleEntity>().CountAsync());

        await context.SaveChangesAsync();
        Assert.Equal(1, await context.Set<ModuleEntity>().CountAsync());
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

    [Fact(DisplayName = "ApplicationDbContext является общим в пределах DI scope")]
    public void ApplicationDbContext_IsSharedWithinScope()
    {
        var services = new ServiceCollection();

        services.AddPlatformEntityFrameworkCore(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        using var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();

        var first = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var second = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        Assert.Same(first, second);
    }

    [Fact(DisplayName = "Репозитории в одном scope используют общий DbContext")]
    public async Task Repositories_InSameScope_ShareContextAndSaveChanges()
    {
        var services = new ServiceCollection();

        services.AddModule<SampleEfModule>(
            new ConfigurationBuilder().Build(),
            "Test");

        services.AddPlatformEntityFrameworkCore(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var scopedServices = scope.ServiceProvider;

        var context = scopedServices.GetRequiredService<ApplicationDbContext>();
        var sameContext = scopedServices.GetRequiredService<ApplicationDbContext>();
        var repository = scopedServices.GetRequiredService<IRepository<ModuleEntity, int>>();
        var efRepository = scopedServices.GetRequiredService<IEfRepository<ModuleEntity, int>>();

        // Контекст в пределах одного scope должен быть одним экземпляром.
        Assert.Same(context, sameContext);

        var first = new ModuleEntity
        {
            Id = 41,
            Name = "Created through IRepository"
        };

        var second = new ModuleEntity
        {
            Id = 42,
            Name = "Created through IEfRepository"
        };

        repository.Add(first);
        efRepository.Add(second);

        // Оба репозитория работают с одним ChangeTracker.
        Assert.Equal(EntityState.Added, context.Entry(first).State);
        Assert.Equal(EntityState.Added, context.Entry(second).State);

        // До SaveChanges изменения ещё не сохранены.
        Assert.Equal(0, await context.Set<ModuleEntity>().CountAsync());

        // Один вызов сохраняет обе сущности.
        await context.SaveChangesAsync();
        Assert.Equal(2, await context.Set<ModuleEntity>().CountAsync());
        Assert.Equal(EntityState.Unchanged, context.Entry(first).State);
        Assert.Equal(EntityState.Unchanged, context.Entry(second).State);
    }

    private sealed class CustomModuleEntityRepository(ApplicationDbContext context)
        : IRepository<ModuleEntity, int>
    {
        private readonly EfRepository<ModuleEntity, int> _inner = new(context);

        public Task<ModuleEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            _inner.GetByIdAsync(id, cancellationToken);

        public Task<IReadOnlyList<ModuleEntity>> ListAsync(
            int skip = 0,
            int take = 100,
            CancellationToken cancellationToken = default) =>
            _inner.ListAsync(skip, take, cancellationToken);

        public void Add(ModuleEntity entity) => throw new CustomRepositoryCalledException();

        public void Update(ModuleEntity entity) => _inner.Update(entity);

        public void Delete(ModuleEntity entity) => _inner.Delete(entity);
    }

    [Fact(DisplayName = "Синхронный SaveChanges заполняет аудит создания и изменения")]
    public void SaveChanges_WhenCreatingAndUpdatingEntity_SetsAuditInformation()
    {
        var createdAt = new DateTimeOffset(2026, 10, 21, 10, 0, 0, TimeSpan.Zero);

        var clock = new FakeTimeProvider(createdAt);
        var services = new ServiceCollection();

        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton<ICurrentUser>(new TestCurrentUser("user-1"));
        services.AddModule<SampleEfModule>(
            new ConfigurationBuilder().Build(),
            "Test");
        services.AddPlatformEntityFrameworkCore(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var entity = new AuditedModuleEntity
        {
            Id = 101,
            Name = "Before update"
        };

        context.Add(entity);
        context.SaveChanges();

        Assert.Equal(createdAt, entity.CreatedAtUtc);
        Assert.Equal("user-1", entity.CreatedByUserId);
        Assert.Null(entity.ModifiedAtUtc);
        Assert.Null(entity.ModifiedByUserId);

        var modifiedAt = createdAt.AddMinutes(5);
        clock.SetUtcNow(modifiedAt);

        entity.Name = "After update";
        context.SaveChanges();

        Assert.Equal(createdAt, entity.CreatedAtUtc);
        Assert.Equal("user-1", entity.CreatedByUserId);
        Assert.Equal(modifiedAt, entity.ModifiedAtUtc);
        Assert.Equal("user-1", entity.ModifiedByUserId);
    }
    
    [Fact(DisplayName = "Повторный SaveChangesAsync не изменяет аудит уже удалённой сущности")]
    public async Task SaveChangesAsync_WhenSavingSoftDeletedEntityAgain_PreservesDeletionAudit()
    {
        var deletedAt = new DateTimeOffset(2026, 10, 21, 10, 0, 0, TimeSpan.Zero);

        var clock = new FakeTimeProvider(deletedAt);
        var services = new ServiceCollection();

        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton<ICurrentUser>(new TestCurrentUser("user-1"));
        services.AddModule<SampleEfModule>(
            new ConfigurationBuilder().Build(),
            "Test");
        services.AddPlatformEntityFrameworkCore(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var entity = new AuditedModuleEntity
        {
            Id = 102,
            Name = "To delete"
        };

        context.Add(entity);
        await context.SaveChangesAsync();

        context.Remove(entity);
        await context.SaveChangesAsync();

        Assert.True(entity.IsDeleted);
        Assert.Equal(deletedAt, entity.DeletedAtUtc);
        Assert.Equal("user-1", entity.DeletedByUserId);

        // Переводим часы вперёд и повторно сохраняем контекст.
        clock.SetUtcNow(deletedAt.AddHours(1));

        await context.SaveChangesAsync();

        Assert.Equal(deletedAt, entity.DeletedAtUtc);
        Assert.Equal("user-1", entity.DeletedByUserId);

        context.ChangeTracker.Clear();

        var stored = await context.Set<AuditedModuleEntity>()
            .IgnoreQueryFilters()
            .SingleAsync(item => item.Id == 102);

        Assert.True(stored.IsDeleted);
        Assert.Equal(deletedAt, stored.DeletedAtUtc);
        Assert.Equal("user-1", stored.DeletedByUserId);
    }
    
    [Fact(DisplayName = "Кэш модели учитывает набор зарегистрированных EF-модулей")]
    public void ModelCache_WhenContextsHaveDifferentModuleSets_UsesDifferentModels()
    {
        var servicesWithModule = new ServiceCollection();

        servicesWithModule.AddModule<SampleEfModule>(new ConfigurationBuilder().Build(), "Test");

        servicesWithModule.AddPlatformEntityFrameworkCore(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        var servicesWithoutModule = new ServiceCollection();

        servicesWithoutModule.AddPlatformEntityFrameworkCore(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        using var providerWithModule = servicesWithModule.BuildServiceProvider();
        using var providerWithoutModule = servicesWithoutModule.BuildServiceProvider();

        using var contextWithModule =
            providerWithModule.GetRequiredService<ApplicationDbContext>();

        using var contextWithoutModule =
            providerWithoutModule.GetRequiredService<ApplicationDbContext>();

        Assert.NotNull(
            contextWithModule.Model.FindEntityType(typeof(ModuleEntity)));

        Assert.Null(
            contextWithoutModule.Model.FindEntityType(typeof(ModuleEntity)));
    }
    
    [Fact(DisplayName = "Кэш модели учитывает набор зарегистрированных EF-модулей в общем EF service provider")]
    public void ModelCache_WhenContextsHaveDifferentModuleSets_UsesDifferentModels2()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ReplaceService<
                Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory,
                ApplicationModelCacheKeyFactory>()
            .Options;

        // Одинаковые options означают общий EF Core internal service provider и общий кэш моделей.
        using var contextWithModule = new ApplicationDbContext(
            options,
            [new SampleEfModule()],
            new StubCurrentUser(),
            TimeProvider.System);

        using var contextWithoutModule = new ApplicationDbContext(
            options,
            Array.Empty<IModule>(),
            new StubCurrentUser(),
            TimeProvider.System);

        Assert.NotNull(contextWithModule.Model.FindEntityType(typeof(ModuleEntity)));
        Assert.Null(contextWithoutModule.Model.FindEntityType(typeof(ModuleEntity)));

        // Повторное создание первого варианта также не должно получить модель второго.
        using var anotherContextWithModule = new ApplicationDbContext(
            options,
            [new SampleEfModule()],
            new StubCurrentUser(),
            TimeProvider.System);

        Assert.NotNull(anotherContextWithModule.Model.FindEntityType(typeof(ModuleEntity)));
    }
    
    private sealed class CustomRepositoryCalledException : Exception
    {
    }

    private sealed class CustomOpenGenericRepository<TEntity, TId>(ApplicationDbContext context)
        : EfRepository<TEntity, TId>(context)
        where TEntity : class, Platform.EntityFrameworkCore.Entities.IEntityId<TId>
    {
    }
    
    private sealed class TestCurrentUser(string userId) : ICurrentUser
    {
        public string? UserId => userId;
    }
}