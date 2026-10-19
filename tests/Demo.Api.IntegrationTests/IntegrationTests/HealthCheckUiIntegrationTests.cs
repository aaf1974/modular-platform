using Demo.Api.IntegrationTests.Infrastructure;
using Shouldly;

namespace Demo.Api.IntegrationTests.IntegrationTests;

/// <summary>
/// Проверяет доступность веб-интерфейса мониторинга состояния приложения
/// и совместимость интерфейса с существующим endpoint проверки состояния.
/// </summary>
/// <param name="fixture">
/// Общая тестовая инфраструктура демонстрационного API.
/// </param>
[Collection(DemoApiCollection.Name)]
public sealed class HealthCheckUiIntegrationTests(DemoApiFixture fixture)
{
    [Fact(DisplayName = "Проверяет, что веб-интерфейс HealthChecks UI возвращает HTML-страницу.")]
    public async Task GetHealthChecksUi_ShouldReturnHtmlPage()
    {
        // Act
        using var response = await fixture.Client.GetAsync("/healthchecks-ui");

        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType
            .ShouldBe("text/html");
        content.ShouldNotBeNullOrWhiteSpace();
        content.ShouldContain("html", Case.Insensitive);
    }

    [Fact(DisplayName = "Проверяет, что endpoint проверки состояния продолжает работать после обращения к веб-интерфейсу HealthChecks UI.")]
    public async Task GetHealth_ShouldRemainHealthyAfterRequestingUi()
    {
        // Act
        using var uiResponse = await fixture.Client.GetAsync("/healthchecks-ui");
        using var healthResponse = await fixture.Client.GetAsync("/health");
        var content = await healthResponse.Content.ReadAsStringAsync();

        // Assert
        uiResponse.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        healthResponse.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        content.ShouldContain("Healthy");
    }
}