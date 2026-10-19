using System.Text.Json;
using Demo.Api.IntegrationTests.Infrastructure;
using Shouldly;

namespace Demo.Api.IntegrationTests.IntegrationTests;

/// <summary>
/// Проверяет доступность Swagger и генерацию OpenAPI-документа
/// в демонстрационном приложении.
/// </summary>
/// <param name="fixture">
/// Общая тестовая инфраструктура демонстрационного API.
/// </param>
[Collection(DemoApiCollection.Name)]
public sealed class SwaggerIntegrationTests(DemoApiFixture fixture)
{
    /// <summary>
    /// Проверяет, что Swagger возвращает действительный OpenAPI-документ,
    /// содержащий корневой endpoint приложения.
    /// </summary>
    [Fact(DisplayName = "GET swagger JSON returns an OpenAPI document with the root endpoint")]
    public async Task GetSwaggerDocument_ShouldReturnOpenApiDocumentWithRootEndpoint()
    {
        // Act
        using var response = await fixture.Client.GetAsync(
            "/swagger/v1/swagger.json");

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        var root = document.RootElement;

        root.TryGetProperty("openapi", out var openApiVersion)
            .ShouldBeTrue();

        openApiVersion.GetString().ShouldNotBeNullOrWhiteSpace();

        root.TryGetProperty("paths", out var paths)
            .ShouldBeTrue();

        paths.ValueKind.ShouldBe(JsonValueKind.Object);

        paths.TryGetProperty("/", out _).ShouldBeTrue();
    }

    /// <summary>
    /// Проверяет, что Swagger UI доступен по стандартному адресу.
    /// </summary>
    [Fact(DisplayName = "GET swagger UI returns a successful response")]
    public async Task GetSwaggerUi_ShouldReturnSuccessStatusCode()
    {
        // Act
        using var response = await fixture.Client.GetAsync(
            "/swagger/index.html");

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();

        content.ShouldContain("swagger");
    }
}