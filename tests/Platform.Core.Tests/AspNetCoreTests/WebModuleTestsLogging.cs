using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Platform.AspNetCore.Modules;

namespace Platform.AspNetCore.Tests;

public partial class WebModuleTests
{
    /// <summary>
    /// Проверяет, что ошибка настройки веб-модуля записывается в журнал
    /// вместе с исходным исключением и именем модуля.
    /// </summary>
    [Fact(DisplayName = "UseModules записывает ошибку настройки веб-модуля в журнал")]
    public void UseModules_WhenWebModuleConfigurationThrows_ShouldLogError()
    {
        // Arrange
        using var loggerProvider = new RecordingLoggerProvider();

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(loggerProvider);
        builder.AddModule<WebModuleTests.ThrowingWebModule>();

        using var app = builder.Build();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => app.UseModules());

        // Assert
        var logEntry = Assert.Single(
            loggerProvider.Entries.Where(entry =>
                entry.Level == LogLevel.Error &&
                entry.Message.Contains(
                    "Ошибка настройки HTTP-конвейера модуля",
                    StringComparison.Ordinal)));

        Assert.Same(exception, logEntry.Exception);
        Assert.Contains("ThrowingWebModule", logEntry.Message);
    }
}


/// <summary>
/// Тестовый поставщик журналирования, сохраняющий записи для проверки.
/// </summary>
internal sealed class RecordingLoggerProvider : ILoggerProvider
{
    /// <summary>
    /// Список записей, полученных во время теста.
    /// </summary>
    public List<RecordedLogEntry> Entries { get; } = [];

    /// <summary>
    /// Создаёт регистратор для указанной категории.
    /// </summary>
    /// <param name="categoryName">Имя категории журнала.</param>
    /// <returns>Регистратор, сохраняющий записи в списке теста.</returns>
    public ILogger CreateLogger(string categoryName)
    {
        ArgumentNullException.ThrowIfNull(categoryName);

        return new RecordingLogger(categoryName, Entries);
    }

    /// <summary>
    /// Освобождает ресурсы поставщика журналирования.
    /// </summary>
    public void Dispose()
    {
    }
}

/// <summary>
/// Тестовый регистратор, сохраняющий сообщения и исключения.
/// </summary>
internal sealed class RecordingLogger : ILogger
{
    private readonly string _categoryName;
    private readonly List<RecordedLogEntry> _entries;

    /// <summary>
    /// Создаёт регистратор с указанной категорией и списком записей.
    /// </summary>
    /// <param name="categoryName">Имя категории журнала.</param>
    /// <param name="entries">Список для сохранения записей.</param>
    public RecordingLogger(
        string categoryName,
        List<RecordedLogEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(categoryName);
        ArgumentNullException.ThrowIfNull(entries);

        _categoryName = categoryName;
        _entries = entries;
    }

    /// <summary>
    /// Начинает область журналирования.
    /// </summary>
    /// <typeparam name="TState">Тип состояния области.</typeparam>
    /// <param name="state">Состояние области.</param>
    /// <returns>Объект области либо null.</returns>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    /// <summary>
    /// Определяет, разрешено ли журналирование указанного уровня.
    /// </summary>
    /// <param name="logLevel">Уровень журнала.</param>
    /// <returns>true, если запись разрешена.</returns>
    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    /// <summary>
    /// Сохраняет сформированную запись журнала.
    /// </summary>
    /// <typeparam name="TState">Тип состояния сообщения.</typeparam>
    /// <param name="logLevel">Уровень журнала.</param>
    /// <param name="eventId">Идентификатор события.</param>
    /// <param name="state">Состояние сообщения.</param>
    /// <param name="exception">Связанное исключение.</param>
    /// <param name="formatter">Функция формирования сообщения.</param>
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        _entries.Add(new RecordedLogEntry(
            _categoryName,
            logLevel,
            formatter(state, exception),
            exception));
    }
}

/// <summary>
/// Содержит данные одной записи тестового журнала.
/// </summary>
/// <param name="CategoryName">Имя категории журнала.</param>
/// <param name="Level">Уровень журналирования.</param>
/// <param name="Message">Текст сообщения.</param>
/// <param name="Exception">Связанное исключение.</param>
internal sealed record RecordedLogEntry(
    string CategoryName,
    LogLevel Level,
    string Message,
    Exception? Exception);