using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Platform.AspNetCore.Configuration;

/// <summary>
/// Содержит настройки инфраструктурных возможностей платформы.
/// </summary>
public sealed class PlatformOptions
{
    private readonly List<IPlatformWebFeature> _webFeatures = [];

    /// <summary>
    /// Получает зарегистрированные инфраструктурные возможности,
    /// настраивающие HTTP-конвейер.
    /// </summary>
    internal IReadOnlyList<IPlatformWebFeature> WebFeatures => _webFeatures;

    /// <summary>
    /// Добавляет инфраструктурную возможность настройки
    /// HTTP-конвейера.
    /// </summary>
    /// <param name="feature">
    /// Инфраструктурная возможность, которую необходимо подключить.
    /// </param>
    /// <returns>
    /// Текущий экземпляр настроек для продолжения конфигурации.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Инфраструктурная возможность равна <see langword="null"/>.
    /// </exception>
    public PlatformOptions AddWebFeature(IPlatformWebFeature feature)
    {
        ArgumentNullException.ThrowIfNull(feature);

        _webFeatures.Add(feature);

        return this;
    }
}

/// <summary>
/// Представляет инфраструктурную возможность веб-платформы,
/// настраивающую сервисы и HTTP-конвейер приложения.
/// </summary>
public interface IPlatformWebFeature
{
    /// <summary>
    /// Определяет порядок настройки HTTP-конвейера. Меньшие значения выполняются раньше.
    /// При равных значениях сохраняется порядок регистрации; features предшествуют веб-модулям.
    /// </summary>
    int Order => 500;

    /// <summary>
    /// Регистрирует сервисы, необходимые инфраструктурной возможности.
    /// </summary>
    /// <param name="services">Коллекция сервисов приложения.</param>
    void ConfigureServices(IServiceCollection services);

    /// <summary>
    /// Настраивает HTTP-конвейер веб-приложения.
    /// </summary>
    /// <param name="app">Построенное веб-приложение.</param>
    void ConfigureApplication(WebApplication app);
}
