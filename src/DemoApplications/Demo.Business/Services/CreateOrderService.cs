using Business.Entities;
using Platform.EntityFrameworkCore;
using Platform.EntityFrameworkCore.Repositories;

namespace Business.Services;

public sealed class CreateOrderService(IRepository<Order, Guid> orderRepository, ApplicationDbContext dbContext)
{
    public async Task<Guid> CreateAsync(string orderNumber, IReadOnlyCollection<CreateOrderDetail> details, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderNumber);
        ArgumentNullException.ThrowIfNull(details);

        var order = new Order
        {
            OrderNumber = orderNumber,
            Details = details.Select(detail => new OrderDetail
            {
                ProductName = detail.ProductName,
                Quantity = detail.Quantity,
                UnitPrice = detail.UnitPrice
            }).ToList()
        };

        orderRepository.Add(order);

        await dbContext.SaveChangesAsync(cancellationToken);

        return order.Id;
    }
}

public sealed record CreateOrderDetail(
    string ProductName,
    int Quantity,
    decimal UnitPrice);