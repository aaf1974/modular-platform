/*using Business.Entities;
using Business.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Platform.EntityFrameworkCore;

namespace Business.Endpoints;

public static class OrderEndpoints
{
    public static WebApplication MapOrderEndpoints(
        this WebApplication app)
    {
        var orders = app.MapGroup("/api/orders")
            .WithTags("Orders");

        orders.MapPost("/", async (
            CreateOrderRequest request,
            CreateOrderService service,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.OrderNumber))
            {
                return Results.BadRequest(new
                {
                    Error = "OrderNumber is required."
                });
            }

            if (request.Details is null || request.Details.Count == 0)
            {
                return Results.BadRequest(new
                {
                    Error = "At least one order detail is required."
                });
            }

            if (request.Details.Any(detail =>
                    string.IsNullOrWhiteSpace(detail.ProductName) ||
                    detail.ProductName.Length > 200 ||
                    detail.Quantity <= 0 ||
                    detail.UnitPrice < 0))
            {
                return Results.BadRequest(new
                {
                    Error = "One or more order details are invalid."
                });
            }

            var details = request.Details
                .Select(detail => new CreateOrderDetail(
                    detail.ProductName,
                    detail.Quantity,
                    detail.UnitPrice))
                .ToArray();

            var id = await service.CreateAsync(
                request.OrderNumber,
                details,
                cancellationToken);

            return Results.Created(
                $"/api/orders/{id}",
                new { Id = id });
        })
        .WithName("CreateOrder");

        orders.MapGet("/{id:guid}", async (
            Guid id,
            ApplicationDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var order = await dbContext.Set<Order>()
                .AsNoTracking()
                .Include(order => order.Details)
                .SingleOrDefaultAsync(
                    order => order.Id == id,
                    cancellationToken);

            if (order is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new
            {
                order.Id,
                order.OrderNumber,
                order.CreatedAtUtc,
                Details = order.Details.Select(detail => new
                {
                    detail.Id,
                    detail.ProductName,
                    detail.Quantity,
                    detail.UnitPrice
                })
            });
        })
        .WithName("GetOrderById");

        return app;
    }
}

public sealed record CreateOrderRequest(
    string OrderNumber,
    IReadOnlyCollection<CreateOrderDetailRequest> Details);

public sealed record CreateOrderDetailRequest(
    string ProductName,
    int Quantity,
    decimal UnitPrice);*/