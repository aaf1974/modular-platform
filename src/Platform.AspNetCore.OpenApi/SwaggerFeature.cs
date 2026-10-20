using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Platform.AspNetCore.Configuration;

namespace Platform.AspNetCore.OpenApi;

/// <summary>
/// Представляет инфраструктурную возможность настройки Swagger и Swagger UI.
/// </summary>
public sealed class SwaggerFeature : IPlatformWebFeature
{
    public int Order => 200;

    /// <summary>
    /// Регистрирует сервисы, необходимые для генерации OpenAPI-документации.
    /// </summary>
    /// <param name="services">Коллекция сервисов приложения.</param>
    public void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
    }

    /// <summary>
    /// Подключает Swagger и Swagger UI в окружении разработки.
    /// </summary>
    /// <param name="app">Построенное веб-приложение.</param>
    public void ConfigureApplication(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        app.UseSwagger();
        app.UseSwaggerUI();
    }
}