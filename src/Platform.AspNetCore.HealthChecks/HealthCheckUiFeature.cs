using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Platform.AspNetCore.Configuration;

namespace Platform.AspNetCore.HealthChecks;

/// <summary>
/// Представляет инфраструктурную возможность публикации
/// веб-интерфейса мониторинга состояния приложения.
/// </summary>
public sealed class HealthCheckUiFeature : IPlatformWebFeature
{
    /// <summary>
    /// Регистрирует сервисы веб-интерфейса и хранилище результатов проверок.
    /// </summary>
    /// <param name="services">Коллекция сервисов приложения.</param>
    public void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services
            .AddHealthChecksUI(options =>
            {
                options.AddHealthCheckEndpoint(
                    "Demo API",
                    "/health/ready");
            })
            .AddInMemoryStorage();
    }

    /// <summary>
    /// Публикует endpoint веб-интерфейса мониторинга состояния приложения.
    /// </summary>
    /// <param name="app">Построенное веб-приложение.</param>
    public void ConfigureApplication(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapHealthChecksUI(options =>
        {
            options.UIPath = "/healthchecks-ui";
        });
    }
}