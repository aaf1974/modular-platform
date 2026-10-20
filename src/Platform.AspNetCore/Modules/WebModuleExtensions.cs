using Microsoft.AspNetCore.Builder;
using Platform.Core.Modules;

namespace Platform.AspNetCore.Modules;

/// <summary>
/// Предоставляет методы расширения для регистрации модулей
/// в ASP.NET Core-приложении.
/// </summary>
public static class WebModuleRegistrationExtensions
{
    /// <summary>
    /// Регистрирует модуль и его сервисы в ASP.NET Core-приложении.
    /// </summary>
    /// <typeparam name="TModule">
    /// Тип регистрируемого модуля.
    /// </typeparam>
    /// <param name="builder">Построитель веб-приложения.</param>
    /// <returns>Исходный построитель веб-приложения.</returns>
    /// <exception cref="ArgumentNullException">
    /// Построитель приложения равен <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Модуль уже зарегистрирован, объявил некорректные зависимости
    /// или произошла ошибка при регистрации его сервисов.
    /// </exception>
    /// <remarks>
    /// Модуль создаётся механизмом Platform.Core.
    /// Если модуль реализует <see cref="IWebModule"/>,
    /// его HTTP-конфигурация будет выполнена позднее
    /// при вызове <see cref="WebModuleApplicationExtensions.UseModules"/>.
    /// </remarks>
    public static WebApplicationBuilder AddModule<TModule>(this WebApplicationBuilder builder)
        where TModule : class, IModule, new()
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddModule<TModule>(
            builder.Configuration,
            builder.Environment.EnvironmentName);

        return builder;
    }

    /// <summary>
    /// Проверяет обязательные зависимости зарегистрированных модулей.
    /// </summary>
    /// <param name="builder">Построитель веб-приложения.</param>
    /// <returns>Исходный построитель веб-приложения.</returns>
    /// <exception cref="ArgumentNullException">
    /// Построитель приложения равен <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Обнаружены отсутствующие обязательные зависимости модулей.
    /// </exception>
    /// <remarks>
    /// Рекомендуется вызывать после регистрации всех модулей
    /// и до вызова Build().
    /// </remarks>
    public static WebApplicationBuilder ValidateModules(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.ValidateModules();

        return builder;
    }
}