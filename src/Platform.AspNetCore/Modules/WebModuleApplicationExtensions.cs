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
    /// <summary>
    /// Таблица слабых связей (Weak-ссылок) для отслеживания состояния вызовов <see cref="UseModules"/> 
    /// на конкретных экземплярах <see cref="WebApplication"/>.
    /// Использование <see cref="ConditionalWeakTable{TKey, TValue}"/> предотвращает утечки памяти (Memory Leaks), 
    /// позволяя сборщику мусора (GC) беспрепятственно удалять экземпляры <see cref="WebApplication"/>, 
    /// когда они выходят из обращения, автоматически очищая связанные с ними данные состояния.
    /// </summary>
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

        app.UseExceptionHandler();
        
        var logger = app.Logger;
        logger.LogInformation("Начата настройка инфраструктурных возможностей и веб-модулей.");

        var features = app.Services.GetServices<IPlatformWebFeature>().ToArray();
        var pipelineItems = CreatePipelineItems(app);

        logger.LogInformation("Настройка HTTP-конвейера: {FeatureCount} возможностей и {ModuleCount} модулей.",
            features.Length,
            pipelineItems.Count(item => item.Kind == 1));

        foreach (var item in pipelineItems)
        {
            logger.LogInformation("Настройка компонента HTTP-конвейера {ComponentName} (Order={Order}).",
                item.Name, item.Order);
            try
            {
                item.Configure();
                logger.LogInformation("Компонент HTTP-конвейера {ComponentName} настроен.", item.Name);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Ошибка настройки компонента HTTP-конвейера {ComponentName}.", item.Name);
                throw;
            }
        }

        logger.LogInformation("Настройка инфраструктурных возможностей и веб-модулей завершена.");
        return app;
    }

        /// <summary>
    /// Извлекает зарегистрированные инфраструктурные возможности (<see cref="IPlatformWebFeature"/>) 
    /// и веб-модули (<see cref="IWebModule"/>) из DI-контейнера приложения и формирует 
    /// единый, строго отсортированный массив элементов конвейера для последующей инициализации.
    /// </summary>
    /// <param name="app">Экземпляр веб-приложения, предоставляющий доступ к DI-контейнеру.</param>
    /// <returns>Отсортированный массив элементов конвейера <see cref="PipelineItem"/>.</returns>
    /// <remarks>
    /// <para>
    /// Сортировка элементов выполняется последовательно по трем критериям:
    /// </para>
    /// <list type="number">
    /// <item>
    /// <description><see cref="PipelineItem.Order"/> — основной явный приоритет компонента (меньше значение — раньше вызов).</description>
    /// </item>
    /// <item>
    /// <description><see cref="PipelineItem.Kind"/> — тип компонента (при одинаковом Order инфраструктурные возможности <c>Kind = 0</c> всегда идут перед веб-модулями <c>Kind = 1</c>).</description>
    /// </item>
    /// <item>
    /// <description><see cref="PipelineItem.RegistrationIndex"/> — исходный порядок регистрации в DI-контейнере для обеспечения детерминированности.</description>
    /// </item>
    /// </list>
    /// </remarks>
    private static PipelineItem[] CreatePipelineItems(WebApplication app)
    {
        var context = new WebModuleContext(app);
        var features = app.Services.GetServices<IPlatformWebFeature>().ToArray();
        var modules = app.Services.GetServices<IModule>().ToArray();

        return features
            .Select((feature, index) => new PipelineItem(
                feature.Order, 0, index, feature.GetType().FullName ?? feature.GetType().Name,
                () => feature.ConfigureApplication(app)))
            .Concat(modules.Select((module, index) => (module, index))
                .Where(item => item.module is IWebModule)
                .Select(item => new PipelineItem(
                    ((IWebModule)item.module).Order, 1, item.index,
                    item.module.GetType().FullName ?? item.module.GetType().Name,
                    () => ((IWebModule)item.module).ConfigureApplication(context))))
            .OrderBy(item => item.Order)
            .ThenBy(item => item.Kind)
            .ThenBy(item => item.RegistrationIndex)
            .ToArray();
    }
        
    /// <summary>
    /// Представляет элемент конвейера middleware с метаданными сортировки
    /// и действием настройки.
    /// </summary>
    /// <param name="Order">
    /// Порядок выполнения. Элементы с меньшим значением настраиваются раньше.
    /// </param>
    /// <param name="Kind">
    /// Тип компонента, используемый для определения порядка между компонентами
    /// разных типов при одинаковом значении <paramref name="Order"/>.
    /// </param>
    /// <param name="RegistrationIndex">
    /// Исходный индекс регистрации, обеспечивающий детерминированный порядок,
    /// если предыдущие критерии сортировки совпадают.
    /// </param>
    /// <param name="Name">
    /// Название компонента, используемое для диагностики и формирования сообщений об ошибках.
    /// </param>
    /// <param name="Configure">
    /// Действие, настраивающее middleware компонента в конвейере приложения.
    /// </param>
    private sealed record PipelineItem(int Order, int Kind, int RegistrationIndex, string Name, Action Configure);

    private sealed class UseModulesState
    {
        public int IsStarted;
    }
}