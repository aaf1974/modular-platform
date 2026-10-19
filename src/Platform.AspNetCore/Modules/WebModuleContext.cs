using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Platform.AspNetCore.Modules;

/// <summary>
/// Предоставляет веб-модулю доступ к построенному ASP.NET Core-приложению.
/// </summary>
public sealed class WebModuleContext
{
    /// <summary>
    /// Получает построенное веб-приложение.
    /// </summary>
    public WebApplication Application { get; }

    /// <summary>
    /// Получает сервисы приложения.
    /// </summary>
    public IServiceProvider Services => Application.Services;

    /// <summary>
    /// Получает конфигурацию приложения.
    /// </summary>
    public IConfiguration Configuration => Application.Configuration;

    /// <summary>
    /// Получает информацию об окружении приложения.
    /// </summary>
    public IHostEnvironment Environment => Application.Environment;

    /// <summary>
    /// Создаёт контекст веб-модуля.
    /// </summary>
    /// <param name="application">Построенное веб-приложение.</param>
    /// <exception cref="ArgumentNullException">
    /// Приложение равно <see langword="null"/>.
    /// </exception>
    public WebModuleContext(WebApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);

        Application = application;
    }
}