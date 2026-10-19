using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Platform.AspNetCore.Configuration;

namespace Platform.AspNetCore.HealthChecks;

/// <summary>
/// Представляет инфраструктурную возможность публикации
/// HTTP-endpoint для проверки состояния приложения.
/// </summary>
public sealed class HealthCheckFeature(Action<IHealthChecksBuilder>? configureHealthChecks = null) 
    : IPlatformWebFeature
{
    /// <summary>
    /// Регистрирует службы проверки состояния приложения.
    /// </summary>
    /// <param name="services">Коллекция служб приложения.</param>
    public void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var healthChecks = services.AddHealthChecks();
        
        configureHealthChecks?.Invoke(healthChecks);
    }

    /// <summary>
    /// Публикует HTTP-endpoint проверки состояния приложения.
    /// </summary>
    /// <param name="app">Построенное веб-приложение.</param>
    public void ConfigureApplication(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
    }
}