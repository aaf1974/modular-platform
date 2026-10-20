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
    public int Order => 100;

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

        // Liveness проверяет только способность процесса отвечать и не запускает проверки зависимостей.
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        // Readiness включает зарегистрированные проверки зависимостей хоста.
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
    }
}