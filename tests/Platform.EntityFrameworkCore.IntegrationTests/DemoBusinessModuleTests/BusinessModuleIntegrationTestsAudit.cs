using Business;
using Business.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Platform.Core.Modules;
using Platform.EntityFrameworkCore.Auditing;
using Platform.EntityFrameworkCore.Repositories;

namespace Platform.EntityFrameworkCore.IntegrationTests.DemoBusinessModuleTests;

public partial class BusinessModuleIntegrationTests
{
    private static readonly DateTimeOffset InitialTime =
        new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

    private ServiceProvider BuildServiceProvider(FakeTimeProvider clock)
    {
        var services = new ServiceCollection();

        services.AddSingleton<TimeProvider>(clock);

        services.AddScoped<ICurrentUser>(_ => new TestCurrentUser("test-user-123"));

        services.AddModule<BusinessModule>(
            new ConfigurationBuilder().Build(),
            "IntegrationTest");

        services.AddPlatformEntityFrameworkCore(
            options => options.UseNpgsql(fixture.ConnectionString));

        return services.BuildServiceProvider();
    }
    
    private sealed class TestCurrentUser(string userId) : ICurrentUser
    {
        public string? UserId => userId;
    }
    
    [Fact(DisplayName = "При создании заказа аудит записывает фиксированное время и пользователя")]
    public async Task Should_Set_Creation_Audit_Using_Fixed_Time()
    {
        var clock = new FakeTimeProvider(InitialTime);

        await using var provider = BuildServiceProvider(clock);
        await using var scope = provider.CreateAsyncScope();

        var services = scope.ServiceProvider;

        var context =
            services.GetRequiredService<ApplicationDbContext>();

        await context.Database.EnsureCreatedAsync();

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

        await using var provider = BuildServiceProvider(clock);
        await using var scope = provider.CreateAsyncScope();

        var services = scope.ServiceProvider;

        var context =
            services.GetRequiredService<ApplicationDbContext>();

        await context.Database.EnsureCreatedAsync();

        var repository =
            services.GetRequiredService<IRepository<Order, Guid>>();

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

        await using var provider = BuildServiceProvider(clock);
        await using var scope = provider.CreateAsyncScope();

        var services = scope.ServiceProvider;

        var context =
            services.GetRequiredService<ApplicationDbContext>();

        await context.Database.EnsureCreatedAsync();

        var repository =
            services.GetRequiredService<IRepository<Order, Guid>>();

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

        await using var provider = BuildServiceProvider(clock);
        await using var scope = provider.CreateAsyncScope();

        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<ApplicationDbContext>();

        await context.Database.EnsureCreatedAsync();

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
}