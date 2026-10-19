using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

    private sealed class CustomRepositoryCalledException : Exception
    {
    }

    private sealed class CustomOpenGenericRepository<TEntity, TId>(ApplicationDbContext context)
        : EfRepository<TEntity, TId>(context)
        where TEntity : class, Platform.EntityFrameworkCore.Entities.IEntityId<TId>
    {
    }
}