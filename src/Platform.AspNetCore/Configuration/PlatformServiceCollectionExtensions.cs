using Microsoft.Extensions.DependencyInjection;
using Platform.AspNetCore.Exceptions;

namespace Platform.AspNetCore.Configuration;

/// <summary>
/// Предоставляет методы расширения для регистрации инфраструктуры платформы.
/// </summary>
public static class PlatformServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует инфраструктурные возможности платформы и необходимые им сервисы.
    /// </summary>
    /// <param name="services">Коллекция сервисов приложения.</param>
    /// <param name="configure">Действие настройки инфраструктурных возможностей.</param>
    /// <returns>Исходная коллекция сервисов.</returns>
    /// <exception cref="ArgumentNullException">
    /// Коллекция сервисов или действие настройки равны <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Один и тот же экземпляр инфраструктурной возможности зарегистрирован повторно.
    /// </exception>
    public static IServiceCollection AddPlatform(
        this IServiceCollection services,
        Action<PlatformOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new PlatformOptions();
        configure(options);

        var registeredFeatures = new HashSet<IPlatformWebFeature>(ReferenceEqualityComparer.Instance);

        foreach (var descriptor in services)
        {
            if (descriptor.ServiceType == typeof(IPlatformWebFeature)
                && descriptor.ImplementationInstance is IPlatformWebFeature feature)
            {
                registeredFeatures.Add(feature);
            }
        }

        // Проверяем все возможности до изменения коллекции сервисов.
        foreach (var feature in options.WebFeatures)
        {
            if (!registeredFeatures.Add(feature))
            {
                throw new InvalidOperationException(
                    $"Инфраструктурная возможность типа " +
                    $"'{feature.GetType().FullName}' уже зарегистрирована. " +
                    "Один экземпляр возможности можно зарегистрировать только один раз.");
            }
        }

        services.AddProblemDetails();
        services.AddExceptionHandler<DefaultExceptionHandler>();
        
        foreach (var feature in options.WebFeatures)
        {
            feature.ConfigureServices(services);
            services.AddSingleton<IPlatformWebFeature>(feature);
        }

        return services;
    }
}