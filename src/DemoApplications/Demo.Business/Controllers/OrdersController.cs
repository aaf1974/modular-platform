using Business.Contracts;
using Business.Services;
using Microsoft.AspNetCore.Mvc;

namespace Business.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(CreateOrderService createOrderService, GetOrderByIdService getOrderByIdService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var details = request.Details
            .Select(detail => new CreateOrderDetail(
                detail.ProductName,
                detail.Quantity,
                detail.UnitPrice))
            .ToArray();

        var id = await createOrderService.CreateAsync(
            request.OrderNumber,
            details,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new { Id = id });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var order = await getOrderByIdService.ExecuteAsync(
            id,
            cancellationToken);

        if (order is null)
        {
            return NotFound();
        }

        var response = new OrderResponse(
            order.Id,
            order.OrderNumber,
            order.CreatedAtUtc,
            order.Details
                .Select(detail => new OrderDetailResponse(
                    detail.Id,
                    detail.ProductName,
                    detail.Quantity,
                    detail.UnitPrice))
                .ToArray());

        return Ok(response);
    }
}

public sealed record CreateOrderRequest(
    string OrderNumber,
    IReadOnlyCollection<CreateOrderDetailRequest> Details);

public sealed record CreateOrderDetailRequest(
    string ProductName,
    int Quantity,
    decimal UnitPrice);