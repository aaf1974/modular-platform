using Business.Entities;
using Microsoft.EntityFrameworkCore;
using Platform.EntityFrameworkCore;

namespace Business.Services;

public sealed class GetOrderByIdService(ApplicationDbContext dbContext)
{
    public async Task<Order?> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<Order>()
            .AsNoTracking()
            .Include(order => order.Details)
            .SingleOrDefaultAsync(order => order.Id == id,
                cancellationToken);
    }
}