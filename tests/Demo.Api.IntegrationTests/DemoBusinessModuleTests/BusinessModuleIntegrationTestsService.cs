using Business.Entities;
using Business.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Platform.EntityFrameworkCore.IntegrationTests.DemoBusinessModuleTests;

public partial class BusinessModuleIntegrationTests
{
    [Fact(DisplayName = "При ошибке сохранения заказа PostgreSQL не сохраняет его частично")]
    public async Task CreateAsync_WhenDetailViolatesConstraint_DoesNotPersistOrder()
    {
        // Arrange
        var orderNumber = $"ORD-{Guid.NewGuid():N}";
        // await using var scope = fixture.Services.CreateAsyncScope();
        // var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        await using (var scope =  fixture.Services.CreateAsyncScope())
        {
            var scopedServices = scope.ServiceProvider;
            var context = scopedServices.GetRequiredService<ApplicationDbContext>();
            await context.Database.EnsureCreatedAsync();

            var service = scope.ServiceProvider.GetRequiredService<CreateOrderService>();

            var details = new[]
            {
                new CreateOrderDetail("Keyboard", 1, 100m),
                // PostgreSQL отклоняет позицию с Quantity = 0, см. OrderDetailConfiguration
                new CreateOrderDetail("Mouse", 0, 50m)
            };

            // Act
            // await Assert.ThrowsAsync<DbUpdateException>(() => service.CreateAsync(orderNumber, details));
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => service.CreateAsync(orderNumber, details));
            
            // Assert
            var postgresException = Assert.IsType<PostgresException>(exception.InnerException);

            Assert.Equal("23514", postgresException.SqlState);
            Assert.Equal("CK_order_details_quantity_positive", postgresException.ConstraintName);
        }

        // Assert: проверяем через новый scope и новый DbContext.
        await using (var scope =  fixture.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var orderExists = await dbContext.Set<Order>()
                .IgnoreQueryFilters()
                .AnyAsync(order => order.OrderNumber == orderNumber);

            Assert.False(orderExists);
        }
    }
}