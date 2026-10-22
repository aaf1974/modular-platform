using Business.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Platform.EntityFrameworkCore.Repositories;

namespace Platform.EntityFrameworkCore.IntegrationTests.DemoBusinessModuleTests;

public partial class BusinessModuleIntegrationTests
{
  
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
            detailId => Assert.Contains(deletedOrder.Details, detail => detail.Id == detailId));
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

        Assert.Equal(EntityState.Deleted, context.Entry(entityToDelete).State);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var deletedEntity = await context.Set<PhysicalDeleteEntity>()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == entityId);

        Assert.Null(deletedEntity);
    }
}