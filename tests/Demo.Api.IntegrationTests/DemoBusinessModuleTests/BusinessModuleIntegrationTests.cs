using Business;
using Business.Entities;
using Demo.Api.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Core.Modules;
using Platform.EntityFrameworkCore.Repositories;

namespace Platform.EntityFrameworkCore.IntegrationTests.DemoBusinessModuleTests;

[Collection(DemoApiCollection.Name)]
public partial class BusinessModuleIntegrationTests(DemoApiFixture  fixture) :  IClassFixture<DemoApiFixture >
{
    [Fact]
    public async Task Should_Save_And_Load_Order_With_Details()
    {
        // Arrange
        await using var scope = fixture.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var order = new Order
        {
            OrderNumber = $"ORD-{Guid.NewGuid():N}",
            Details =
            [
                new OrderDetail
                {
                    ProductName = "Keyboard",
                    Quantity = 2,
                    UnitPrice = 100m
                },
                new OrderDetail
                {
                    ProductName = "Mouse",
                    Quantity = 1,
                    UnitPrice = 50m
                }
            ]
        };

        dbContext.Set<Order>().Add(order);

        // Act: заказ и обе позиции сохраняются одной операцией.
        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();

        var loadedOrder = await dbContext.Set<Order>()
            .Include(x => x.Details)
            .SingleAsync(x => x.Id == order.Id);

        // Assert
        Assert.Equal(order.OrderNumber, loadedOrder.OrderNumber);
        Assert.Equal(2, loadedOrder.Details.Count);

        Assert.Contains(
            loadedOrder.Details,
            x => x.ProductName == "Keyboard"
                 && x.Quantity == 2
                 && x.UnitPrice == 100m);

        Assert.Contains(
            loadedOrder.Details,
            x => x.ProductName == "Mouse"
                 && x.Quantity == 1
                 && x.UnitPrice == 50m);
    }

    [Fact(DisplayName = "Репозиторий добавляет заказ без сохранения до вызова SaveChangesAsync")]
    public async Task Should_Add_Order_Without_Saving_Changes()
    {
        // Arrange
        await using var scope = fixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<Order, Guid>>();

        var order = new Order
        {
            OrderNumber = $"ORD-{Guid.NewGuid():N}",
            Details =
            [
                new OrderDetail
                {
                    ProductName = "Keyboard",
                    Quantity = 2,
                    UnitPrice = 100m
                }
            ]
        };

        // Act
        repository.Add(order);

        // Assert: репозиторий отслеживает заказ, но ещё не сохраняет его.
        Assert.Equal(EntityState.Added, context.Entry(order).State);

        Assert.Equal(0, await context.Set<Order>().CountAsync(x => x.Id == order.Id));

        // Act: сохраняем изменения через тот же контекст.
        await context.SaveChangesAsync();

        // Assert: теперь заказ существует в БД.
        Assert.Equal(1, await context.Set<Order>().CountAsync(x => x.Id == order.Id));
    }

    [Fact(DisplayName = "Репозиторий обновляет заказ и сохраняет изменения в PostgreSQL")]
    public async Task Should_Update_Order_And_Save_Changes()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var scopedServices = scope.ServiceProvider;

        var context = scopedServices.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();

        var repository =
            scopedServices.GetRequiredService<IRepository<Order, Guid>>();

        var order = new Order
        {
            OrderNumber = $"ORD-{Guid.NewGuid():N}",
            Details =
            [
                new OrderDetail
                {
                    ProductName = "Keyboard",
                    Quantity = 2,
                    UnitPrice = 100m
                }
            ]
        };

        repository.Add(order);
        await context.SaveChangesAsync();

        var originalOrderNumber = order.OrderNumber;
        var updatedOrderNumber = $"UPDATED-{Guid.NewGuid():N}";

        // Отсоединяем сущность, чтобы проверить повторное получение из БД.
        context.ChangeTracker.Clear();

        var orderToUpdate = await repository.GetByIdAsync(order.Id);

        Assert.NotNull(orderToUpdate);
        Assert.Equal(originalOrderNumber, orderToUpdate.OrderNumber);

        orderToUpdate.OrderNumber = updatedOrderNumber;

        repository.Update(orderToUpdate);

        Assert.Equal(
            EntityState.Modified,
            context.Entry(orderToUpdate).State);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var updatedOrder = await context.Set<Order>()
            .SingleAsync(x => x.Id == order.Id);

        Assert.Equal(updatedOrderNumber, updatedOrder.OrderNumber);
    }

    [Fact(DisplayName = "Soft delete скрывает заказ из обычных запросов, сохраняя его в базе")]
    public async Task Should_Soft_Delete_Order_And_Keep_It_In_Database()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var scopedServices = scope.ServiceProvider;
        var context = scopedServices.GetRequiredService<ApplicationDbContext>();

        var repository = scopedServices.GetRequiredService<IEfRepository<Order, Guid>>();

        var order = new Order
        {
            OrderNumber = $"ORD-{Guid.NewGuid():N}"
        };

        repository.Add(order);
        await context.SaveChangesAsync();

        var orderId = order.Id;

        context.ChangeTracker.Clear();

        var orderToDelete = await repository.GetByIdAsync(orderId);

        Assert.NotNull(orderToDelete);

        repository.Delete(orderToDelete);

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var visibleOrder = await repository.Query()
            .SingleOrDefaultAsync(x => x.Id == orderId);

        var deletedOrder = await repository.QueryIncludingDeleted()
            .SingleOrDefaultAsync(x => x.Id == orderId);

        Assert.Null(visibleOrder);
        Assert.NotNull(deletedOrder);
        Assert.True(deletedOrder.IsDeleted);
    }

    [Fact(DisplayName = "Soft delete заказа сохраняет его детали в базе данных")]
    public async Task Should_Soft_Delete_Order_And_Preserve_Details()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var scopedServices = scope.ServiceProvider;

        var context = scopedServices.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();

        var repository =
            scopedServices.GetRequiredService<IEfRepository<Order, Guid>>();

        var order = new Order
        {
            OrderNumber = $"ORD-{Guid.NewGuid():N}",
            Details =
            [
                new OrderDetail
                {
                    ProductName = "Keyboard",
                    Quantity = 2,
                    UnitPrice = 100m
                },
                new OrderDetail
                {
                    ProductName = "Mouse",
                    Quantity = 1,
                    UnitPrice = 50m
                }
            ]
        };

        repository.Add(order);
        await context.SaveChangesAsync();

        var orderId = order.Id;
        var detailIds = order.Details.Select(x => x.Id).ToArray();

        context.ChangeTracker.Clear();

        var orderToDelete = await repository.Query()
            .Include(x => x.Details)
            .SingleAsync(x => x.Id == orderId);

        repository.Delete(orderToDelete);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var deletedOrder = await repository.QueryIncludingDeleted()
            .Include(x => x.Details)
            .SingleAsync(x => x.Id == orderId);

        Assert.True(deletedOrder.IsDeleted);
        Assert.Equal(2, deletedOrder.Details.Count);

        Assert.All(
            detailIds,
            detailId => Assert.Contains(
                deletedOrder.Details,
                detail => detail.Id == detailId));
    }
    
    [Fact(DisplayName = "Репозиторий физически удаляет сущность без поддержки soft delete")]
    public async Task Should_Physically_Delete_Entity_Without_Soft_Delete()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var scopedServices = scope.ServiceProvider;

        var context = scopedServices.GetRequiredService<ApplicationDbContext>();

        var repository = scopedServices.GetRequiredService<IRepository<PhysicalDeleteEntity, Guid>>();

        var entity = new PhysicalDeleteEntity
        {
            Name = $"Entity-{Guid.NewGuid():N}"
        };

        repository.Add(entity);
        await context.SaveChangesAsync();

        var entityId = entity.Id;

        context.ChangeTracker.Clear();

        var entityToDelete = await repository.GetByIdAsync(entityId);

        Assert.NotNull(entityToDelete);

        repository.Delete(entityToDelete);

        Assert.Equal(
            EntityState.Deleted,
            context.Entry(entityToDelete).State);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var deletedEntity = await context.Set<PhysicalDeleteEntity>()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == entityId);

        Assert.Null(deletedEntity);
    }
}