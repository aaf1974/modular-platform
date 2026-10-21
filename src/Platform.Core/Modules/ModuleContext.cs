using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Platform.Core.Modules;

/// <summary>
/// Предоставляет модулю контекст для регистрации сервисов.
/// </summary>
public sealed class ModuleContext
{
    /// <summary>
    /// Получает коллекцию сервисов приложения.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Получает конфигурацию приложения.
    /// </summary>
    public IConfiguration Configuration { get; }

    /// <summary>
    /// Получает имя текущего окружения приложения.
    /// </summary>
    public string EnvironmentName { get; }

    /// <summary>
    /// Создаёт контекст регистрации модуля.
    /// </summary>
    /// <param name="services">Коллекция сервисов приложения.</param>
    /// <param name="configuration">Конфигурация приложения.</param>
    /// <param name="environmentName">Имя окружения приложения.</param>
    /// <exception cref="ArgumentNullException">
    /// Один из переданных аргументов равен <see langword="null"/>.
    /// </exception>
    public ModuleContext(IServiceCollection services, IConfiguration configuration, string environmentName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);

        Services = services;
        Configuration = configuration;
        EnvironmentName = environmentName;
    }
}