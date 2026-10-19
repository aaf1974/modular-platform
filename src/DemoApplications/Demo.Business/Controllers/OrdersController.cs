using Business.Services;
using Microsoft.AspNetCore.Mvc;

namespace Business.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(CreateOrderService createOrderService) : ControllerBase
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
    public async Task<IActionResult> GetById(Guid id)
    {
        // Здесь добавим чтение заказа через отдельный query-сервис
        // или репозиторий, когда определим нужный контракт.
        return Ok(new { Id = id });
    }
}

public sealed record CreateOrderRequest(
    string OrderNumber,
    IReadOnlyCollection<CreateOrderDetailRequest> Details);

public sealed record CreateOrderDetailRequest(
    string ProductName,
    int Quantity,
    decimal UnitPrice);