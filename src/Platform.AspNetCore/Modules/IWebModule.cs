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
/// Порядок вызова ConfigureApplication определяется порядком регистрации
/// модулей. Положение добавляемого middleware в HTTP-конвейере определяется
/// порядком вызовов методов конфигурации ASP.NET Core.
/// </remarks>
public interface IWebModule
{
    /// <summary>
    /// Настраивает HTTP-конвейер и endpoints приложения.
    /// </summary>
    /// <param name="context">
    /// Контекст, предоставляющий доступ к построенному веб-приложению.
    /// </param>
    void ConfigureApplication(WebModuleContext context);
}