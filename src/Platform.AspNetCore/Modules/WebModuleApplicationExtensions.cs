using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Platform.AspNetCore.Configuration;
using Platform.Core.Modules;

namespace Platform.AspNetCore.Modules;

/// <summary>
/// Предоставляет методы расширения для настройки HTTP-функциональности модулей.
/// </summary>
public static class WebModuleApplicationExtensions
{
    private static readonly ConditionalWeakTable<WebApplication, UseModulesState> ApplicationStates = new();

    /// <summary>
    /// Выполняет настройку HTTP-функциональности зарегистрированных веб-модулей
    /// и инфраструктурных возможностей платформы.
    /// </summary>
    /// <param name="app">Построенное веб-приложение.</param>
    /// <returns>Исходное веб-приложение.</returns>
    /// <exception cref="ArgumentNullException">
    /// Веб-приложение равно <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Метод уже вызывался для данного экземпляра веб-приложения.
    /// </exception>
    /// <remarks>
    /// Инфраструктурные возможности настраиваются перед веб-модулями.
    /// Настройка выполняется в порядке регистрации.
    /// Повторный вызов запрещён, даже если предыдущая настройка завершилась
    /// исключением, поскольку часть компонентов могла уже изменить конвейер.
    /// </remarks>
    public static WebApplication UseModules(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var state = ApplicationStates.GetValue(app, static _ => new UseModulesState());
        if (Interlocked.Exchange(ref state.IsStarted, 1) != 0)
        {
            throw new InvalidOperationException("Метод UseModules() уже вызывался для данного веб-приложения.");
        }

        var logger = app.Logger;
        logger.LogInformation("Начата настройка инфраструктурных возможностей и веб-модулей.");

        var context = new WebModuleContext(app);
        var features = app.Services
            .GetServices<IPlatformWebFeature>()
            .ToArray();

        logger.LogInformation("Обнаружено инфраструктурных возможностей платформы: {FeatureCount}.", features.Length);

        foreach (var feature in features)
        {
            var featureName = feature.GetType().FullName
                              ?? feature.GetType().Name;

            logger.LogInformation("Настройка инфраструктурной возможности {FeatureName}.", featureName);

            try
            {
                feature.ConfigureApplication(app);
                logger.LogInformation("Инфраструктурная возможность {FeatureName} настроена.", featureName);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Ошибка настройки инфраструктурной возможности {FeatureName}.", featureName);
                throw;
            }
        }

        var modules = app.Services.GetServices<IModule>().ToArray();

        logger.LogInformation("Обнаружено зарегистрированных модулей: {ModuleCount}.", modules.Length);

        foreach (var module in modules)
        {
            var moduleName = module.GetType().FullName
                             ?? module.GetType().Name;

            if (module is not IWebModule webModule)
            {
                logger.LogInformation("Модуль {ModuleName} не реализует IWebModule. " +
                                      "Настройка HTTP-конвейера для него не требуется.",
                    moduleName);

                continue;
            }

            logger.LogInformation("Настройка HTTP-конвейера модуля {ModuleName}.", moduleName);

            try
            {
                webModule.ConfigureApplication(context);

                logger.LogInformation("HTTP-конвейер модуля {ModuleName} настроен.", moduleName);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Ошибка настройки HTTP-конвейера модуля {ModuleName}.", moduleName);

                throw;
            }
        }

        logger.LogInformation("Настройка инфраструктурных возможностей и веб-модулей завершена.");

        return app;
    }

    private sealed class UseModulesState
    {
        public int IsStarted;
    }
}