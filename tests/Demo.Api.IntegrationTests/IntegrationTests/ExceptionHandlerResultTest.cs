using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Demo.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Shouldly;

[Collection(DemoApiCollection.Name)]
public sealed class ExceptionHandlerResultTest(DemoApiFixture fixture)
{
    [Fact(DisplayName = "POST orders returns exception details in Development when database constraint is violated")]
    public async Task CreateOrder_ShouldReturnExceptionDetails_WhenDatabaseConstraintIsViolatedInDevelopment()
    {
        // Arrange
        using var factory = fixture.Factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
        });

        using var client = factory.CreateClient();

        var orderNumber = $"ERR-DEV-{Guid.NewGuid():N}";

        var request = new
        {
            OrderNumber = orderNumber,
            Details = new[]
            {
                new
                {
                    ProductName = "Keyboard",
                    Quantity = 0,
                    UnitPrice = 49.99m
                }
            }
        };

        // Act
        using var response = await client.PostAsJsonAsync(
            "/api/orders",
            request);

        var responseJson = await response.Content
            .ReadFromJsonAsync<JsonDocument>();

        // Assert
        response.StatusCode.ShouldBe(
            HttpStatusCode.InternalServerError);

        responseJson.ShouldNotBeNull();

        var root = responseJson.RootElement;

        root.GetProperty("status").GetInt32()
            .ShouldBe(StatusCodes.Status500InternalServerError);

        root.GetProperty("detail").GetString()
            .ShouldNotBeNullOrWhiteSpace();

        root.GetProperty("exceptionType").GetString()
            .ShouldNotBeNullOrWhiteSpace();

        root.GetProperty("stackTrace").GetString()
            .ShouldNotBeNullOrWhiteSpace();

        root.GetProperty("traceId").GetString()
            .ShouldNotBeNullOrWhiteSpace();
    }
}  