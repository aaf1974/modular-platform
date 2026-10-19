namespace Business.Contracts;

public sealed record OrderResponse(
    Guid Id,
    string OrderNumber,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyCollection<OrderDetailResponse> Details);

public sealed record OrderDetailResponse(
    Guid Id,
    string ProductName,
    int Quantity,
    decimal UnitPrice);