using System.Text.Json;
using Demo.Api.IntegrationTests.Infrastructure;
using Shouldly;

namespace Demo.Api.IntegrationTests.IntegrationTests;

/// <summary>
/// Проверяет HTTP-endpoint проверки состояния демонстрационного приложения.
/// </summary>
/// <param name="fixture">
/// Общая тестовая инфраструктура демонстрационного API.
/// </param>
[Collection(DemoApiCollection.Name)]
public sealed class HealthCheckIntegrationTests(DemoApiFixture fixture)
{
    [Fact(DisplayName = "Проверяет, что endpoint состояния приложения возвращает успешный ответ.")]
    public async Task GetHealth_ShouldReturnOkAndHealthyStatus()
    {
        // Act
        using var response = await fixture.Client.GetAsync("/health");

        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        content.ShouldContain("Healthy");
    }

    /// <summary>
    /// Проверяет, что endpoint возвращает JSON-отчёт с общим статусом
    /// и результатами зарегистрированных проверок.
    /// </summary>
    [Fact(DisplayName = "GET health returns a detailed JSON health report")]
    public async Task GetHealth_ShouldReturnDetailedJsonReport()
    {
        // Act
        using var response = await fixture.Client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType
            .ShouldBe("application/json");

        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;

        root.GetProperty("status").GetString().ShouldBe("Healthy");
        root.GetProperty("entries").ValueKind
            .ShouldBe(JsonValueKind.Object);
    }

    /// <summary>
    /// Проверяет, что зарегистрированная через AddNpgSql проверка
    /// действительно подключается к тестовой базе PostgreSQL.
    /// </summary>
    [Fact(DisplayName = "GET health reports PostgreSQL check as healthy")]
    public async Task GetHealth_ShouldReportPostgresAsHealthy()
    {
        // Act
        using var response = await fixture.Client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);

        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;
        var entries = root.GetProperty("entries");

        entries.TryGetProperty("npgsql", out var postgresCheck)
            .ShouldBeTrue();

        postgresCheck.GetProperty("status").GetString()
            .ShouldBe("Healthy");
    }
}