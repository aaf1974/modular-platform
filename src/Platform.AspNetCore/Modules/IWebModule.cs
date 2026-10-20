namespace Platform.AspNetCore.Modules;

/// <summary>
/// Определяет контракт модуля, настраивающего HTTP-конвейер приложения.
/// </summary>
/// <remarks>
/// Реализация интерфейса необязательна. Модули без HTTP-функциональности
/// могут ограничиваться контрактом Platform.Core.Modules.IModule.
///
/// Веб-модуль может добавлять middleware, регистрировать endpoints
/// и выполнять другую конфигурацию ASP.NET Core.
///
/// Порядок вызова ConfigureApplication определяется свойством Order, затем порядком регистрации.
/// </remarks>
public interface IWebModule
{
    /// <summary>
    /// Определяет порядок настройки HTTP-конвейера. Меньшие значения выполняются раньше.
    /// При равных значениях сохраняется порядок регистрации; features предшествуют веб-модулям.
    /// </summary>
    int Order => 500;

    /// <summary>
    /// Настраивает HTTP-конвейер и endpoints приложения.
    /// </summary>
    /// <param name="context">
    /// Контекст, предоставляющий доступ к построенному веб-приложению.
    /// </param>
    void ConfigureApplication(WebModuleContext context);
}