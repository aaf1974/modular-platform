using Business.Entities;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Platform.EntityFrameworkCore.Auditing;
using Platform.EntityFrameworkCore.Repositories;

namespace Platform.EntityFrameworkCore.IntegrationTests.DemoBusinessModuleTests;

public partial class BusinessModuleIntegrationTests
{
    private static readonly DateTimeOffset InitialTime = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

    private sealed class TestCurrentUser(string userId) : ICurrentUser
    {
        public string UserId => userId;
    }

    [Fact(DisplayName = "При создании заказа аудит записывает фиксированное время и пользователя")]
    public async Task Should_Set_Creation_Audit_Using_Fixed_Time()
    {
        var clock = new FakeTimeProvider(InitialTime);

        // 1. Создаем кастомизированную фабрику для этого конкретного теста
        // fixture.Factory должен быть доступен (можно сделать public в DemoApiFixture или использовать наследника)
        await using var customizedFactory = fixture.Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // 2. Подменяем реальные сервисы на фейки для теста аудит-полей
                services.AddSingleton<TimeProvider>(clock);
                services.AddScoped<ICurrentUser>(_ => new TestCurrentUser("test-user-123"));
            });
        });

        // 3. Создаем Scope из кастомизированного приложения
        await using var scope = customizedFactory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ApplicationDbContext>();
        var repository = services.GetRequiredService<IRepository<Order, Guid>>();

        var order = new Order
        {
            OrderNumber = $"ORD-{Guid.NewGuid():N}"
        };

        repository.Add(order);

        await context.SaveChangesAsync();

        Assert.Equal(InitialTime, order.CreatedAtUtc);
        Assert.Equal("test-user-123", order.CreatedByUserId);

        Assert.Null(order.ModifiedAtUtc);
        Assert.Null(order.ModifiedByUserId);
    }

    [Fact(DisplayName = "При обновлении заказа аудит меняет время изменения, сохраняя поля создания")]
    public async Task Should_Update_Modification_Audit_Without_Changing_Creation_Audit()
    {
        var clock = new FakeTimeProvider(InitialTime);

        await using var customizedFactory = fixture.Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // 2. Подменяем реальные сервисы на фейки для теста аудит-полей
                services.AddSingleton<TimeProvider>(clock);
                services.AddScoped<ICurrentUser>(_ => new TestCurrentUser("test-user-123"));
            });
        });
        await using var scope = customizedFactory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ApplicationDbContext>();
        var repository = services.GetRequiredService<IRepository<Order, Guid>>();

        var order = new Order
        {
            OrderNumber = $"ORD-{Guid.NewGuid():N}"
        };

        repository.Add(order);
        await context.SaveChangesAsync();

        var createdAt = order.CreatedAtUtc;
        var createdBy = order.CreatedByUserId;

        var modificationTime = InitialTime.AddMinutes(5);
        clock.SetUtcNow(modificationTime);

        order.OrderNumber = $"UPDATED-{Guid.NewGuid():N}";
        repository.Update(order);

        await context.SaveChangesAsync();

        Assert.Equal(createdAt, order.CreatedAtUtc);
        Assert.Equal(createdBy, order.CreatedByUserId);

        Assert.Equal(modificationTime, order.ModifiedAtUtc);
        Assert.Equal("test-user-123", order.ModifiedByUserId);
    }

    [Fact(DisplayName = "При мягком удалении заказа записываются время и пользователь удаления")]
    public async Task Should_Set_Soft_Delete_Audit_Using_Fixed_Time()
    {
        var clock = new FakeTimeProvider(InitialTime);

        await using var customizedFactory = fixture.Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // 2. Подменяем реальные сервисы на фейки для теста аудит-полей
                services.AddSingleton<TimeProvider>(clock);
                services.AddScoped<ICurrentUser>(_ => new TestCurrentUser("test-user-123"));
            });
        });
        await using var scope = customizedFactory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<ApplicationDbContext>();
        var repository = services.GetRequiredService<IRepository<Order, Guid>>();

        var order = new Order
        {
            OrderNumber = $"ORD-{Guid.NewGuid():N}"
        };

        repository.Add(order);
        await context.SaveChangesAsync();

        var deletionTime = InitialTime.AddMinutes(10);
        clock.SetUtcNow(deletionTime);

        repository.Delete(order);
        await context.SaveChangesAsync();

        Assert.True(order.IsDeleted);
        Assert.Equal(deletionTime, order.DeletedAtUtc);
        Assert.Equal("test-user-123", order.DeletedByUserId);

        Assert.Null(await repository.GetByIdAsync(order.Id));
    }

    [Fact(DisplayName = "Повторное сохранение неизменённого заказа не обновляет аудит")]
    public async Task Should_Not_Update_Audit_When_Entity_Is_Unchanged()
    {
        var clock = new FakeTimeProvider(InitialTime);

        await using var customizedFactory = fixture.Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // 2. Подменяем реальные сервисы на фейки для теста аудит-полей
                services.AddSingleton<TimeProvider>(clock);
                services.AddScoped<ICurrentUser>(_ => new TestCurrentUser("test-user-123"));
            });
        });
        await using var scope = customizedFactory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<ApplicationDbContext>();
        var repository = services.GetRequiredService<IRepository<Order, Guid>>();

        var order = new Order
        {
            OrderNumber = $"ORD-{Guid.NewGuid():N}"
        };

        repository.Add(order);
        await context.SaveChangesAsync();

        var createdAt = order.CreatedAtUtc;
        var modifiedAt = order.ModifiedAtUtc;
        var modifiedBy = order.ModifiedByUserId;

        clock.SetUtcNow(InitialTime.AddHours(1));

        await context.SaveChangesAsync();

        Assert.Equal(createdAt, order.CreatedAtUtc);
        Assert.Equal(modifiedAt, order.ModifiedAtUtc);
        Assert.Equal(modifiedBy, order.ModifiedByUserId);
    }

    [Fact(DisplayName = "Аудит мягкого удаления сохраняется в PostgresSQL")]
    public async Task Should_Persist_Soft_Delete_Audit_In_Database()
    {
        var clock = new FakeTimeProvider(InitialTime);

        await using var customizedFactory =
            fixture.Factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton<TimeProvider>(clock);
                    services.AddScoped<ICurrentUser>(_ => new TestCurrentUser("test-user-123"));
                });
            });

        await using var scope =
            customizedFactory.Services.CreateAsyncScope();

        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ApplicationDbContext>();
        var repository =
            services.GetRequiredService<IRepository<Order, Guid>>();

        var order = new Order
        {
            OrderNumber = $"ORD-{Guid.NewGuid():N}"
        };

        repository.Add(order);
        await context.SaveChangesAsync();

        var orderId = order.Id;
        var deletionTime = InitialTime.AddMinutes(10);

        clock.SetUtcNow(deletionTime);

        repository.Delete(order);
        await context.SaveChangesAsync();

        // Убираем отслеживаемый объект, чтобы проверить данные из БД.
        context.ChangeTracker.Clear();

        var persistedOrder = await context.Set<Order>()
            .IgnoreQueryFilters()
            .SingleAsync(x => x.Id == orderId);

        Assert.True(persistedOrder.IsDeleted);
        Assert.Equal(deletionTime, persistedOrder.DeletedAtUtc);
        Assert.Equal("test-user-123", persistedOrder.DeletedByUserId);
    }
}