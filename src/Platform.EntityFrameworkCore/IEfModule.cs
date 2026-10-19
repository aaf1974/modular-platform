using System.Reflection;

namespace Platform.EntityFrameworkCore;

/// <summary>
/// Предоставляет сборки с конфигурациями сущностей модуля для EF Core.
/// </summary>
/// <remarks>
/// Реализация интерфейса необязательна. Модули, не использующие EF Core,
/// не обязаны его реализовывать.
///
/// Модуль может предоставить несколько сборок, если его EF-конфигурации
/// разделены между несколькими проектами.
///
/// Применение конфигураций к общей модели выполняет приложение
/// или инфраструктурное расширение Platform.EntityFrameworkCore.
/// </remarks>
public interface IEfModule
{
    /// <summary>
    /// Получает сборки, содержащие конфигурации сущностей модуля.
    /// </summary>
    IReadOnlyCollection<Assembly> ConfigurationAssemblies { get; }
}