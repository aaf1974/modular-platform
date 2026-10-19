using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Business.Contracts;
using Business.Entities;
using Demo.Api.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Platform.EntityFrameworkCore;
using Shouldly;

namespace Demo.Api.IntegrationTests.IntegrationTests;

[Collection(DemoApiCollection.Name)]
public sealed class OrdersControllerTests(DemoApiFixture fixture)
{
    [Fact(DisplayName = "POST orders creates an order and persists it in PostgreSQL")]
    public async Task CreateOrder_ShouldPersistOrder_WhenRequestIsValid()
    {
        // Arrange
        var orderNumber = $"INT-{Guid.NewGuid()}";

        var request = new
        {
            OrderNumber = orderNumber,
            Details = new[]
            {
                new
                {
                    ProductName = "Keyboard",
                    Quantity = 2,
                    UnitPrice = 49.99m
                },
                new
                {
                    ProductName = "Mouse",
                    Quantity = 1,
                    UnitPrice = 19.50m
                }
            }
        };

        // Act
        using var response = await fixture.Client.PostAsJsonAsync(
            "/api/orders",
            request);

        // Assert: HTTP contract
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var responseJson = await response.Content
            .ReadFromJsonAsync<JsonDocument>();

        responseJson.ShouldNotBeNull();

        responseJson.RootElement
            .TryGetProperty("id", out var idElement)
            .ShouldBeTrue();

        var orderId = idElement.GetGuid();
        orderId.ShouldNotBe(Guid.Empty);

        // Assert: data was actually persisted in PostgreSQL
        await using var scope = fixture.Services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var persistedOrder = await dbContext.Set<Order>()
            .Include(order => order.Details)
            .SingleOrDefaultAsync(order => order.Id == orderId);

        persistedOrder.ShouldNotBeNull();
        persistedOrder.OrderNumber.ShouldBe(orderNumber);
        persistedOrder.Details.Count.ShouldBe(2);

        persistedOrder.Details.ShouldContain(detail =>
            detail.ProductName == "Keyboard"
            && detail.Quantity == 2
            && detail.UnitPrice == 49.99m);

        persistedOrder.Details.ShouldContain(detail =>
            detail.ProductName == "Mouse"
            && detail.Quantity == 1
            && detail.UnitPrice == 19.50m);
    }
    
    [Fact(DisplayName = "GET orders returns order with details when order exists")]
    public async Task GetOrder_ShouldReturnOrderWithDetails_WhenOrderExists()
    {
        // Arrange: create an order through the public API.
        var orderNumber = $"GET-{Guid.NewGuid():N}";

        var createRequest = new
        {
            OrderNumber = orderNumber,
            Details = new[]
            {
                new
                {
                    ProductName = "Monitor",
                    Quantity = 2,
                    UnitPrice = 199.99m
                }
            }
        };

        using var createResponse = await fixture.Client.PostAsJsonAsync(
            "/api/orders",
            createRequest);

        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await createResponse.Content
            .ReadFromJsonAsync<JsonDocument>();

        created.ShouldNotBeNull();

        created.RootElement
            .TryGetProperty("id", out var idElement)
            .ShouldBeTrue();

        var orderId = idElement.GetGuid();

        // Act
        using var response = await fixture.Client.GetAsync(
            $"/api/orders/{orderId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var order = await response.Content
            .ReadFromJsonAsync<OrderResponse>();

        order.ShouldNotBeNull();
        order.Id.ShouldBe(orderId);
        order.OrderNumber.ShouldBe(orderNumber);
        order.Details.Count.ShouldBe(1);

        var detail = order.Details.Single();
        detail.ProductName.ShouldBe("Monitor");
        detail.Quantity.ShouldBe(2);
        detail.UnitPrice.ShouldBe(199.99m);
    }

    [Fact(DisplayName = "GET orders returns 404 when order does not exist")]
    public async Task GetOrder_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        var missingId = Guid.NewGuid();

        // Act
        using var response = await fixture.Client.GetAsync(
            $"/api/orders/{missingId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}