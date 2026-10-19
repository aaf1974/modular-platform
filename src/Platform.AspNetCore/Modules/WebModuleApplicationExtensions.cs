using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Platform.Core.Modules;

namespace Platform.AspNetCore.Modules;

/// <summary>
/// Предоставляет методы расширения для настройки HTTP-функциональности модулей.
/// </summary>
public static class WebModuleApplicationExtensions
{
    private static readonly ConditionalWeakTable<WebApplication, UseModulesState>
        ApplicationStates = new();

    /// <summary>
    /// Выполняет настройку HTTP-функциональности зарегистрированных веб-модулей.
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
    /// Обрабатываются только зарегистрированные экземпляры модулей,
    /// реализующие <see cref="IWebModule"/>.
    /// Настройка выполняется в порядке регистрации модулей.
    /// Экземпляры модулей повторно не создаются.
    ///
    /// Повторный вызов запрещён даже в случае, если предыдущая настройка
    /// завершилась исключением: часть модулей могла успеть изменить
    /// конфигурацию приложения, поэтому безопасный повторный запуск
    /// гарантировать невозможно.
    /// </remarks>
    public static WebApplication UseModules(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var state = ApplicationStates.GetValue(
            app,
            static _ => new UseModulesState());

        if (Interlocked.Exchange(ref state.IsStarted, 1) != 0)
        {
            throw new InvalidOperationException(
                "Метод UseModules() уже вызывался для данного веб-приложения.");
        }

        var context = new WebModuleContext(app);
        var modules = app.Services.GetServices<IModule>();

        foreach (var module in modules)
        {
            if (module is IWebModule webModule)
            {
                webModule.ConfigureApplication(context);
            }
        }

        return app;
    }

    private sealed class UseModulesState
    {
        public int IsStarted;
    }
}