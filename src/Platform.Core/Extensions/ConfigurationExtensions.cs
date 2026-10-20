using Microsoft.Extensions.Configuration;

namespace Platform.Core;

/// <summary>
/// Методы расширения для <see cref="IConfiguration"/>
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Имя ключа в конфигурации для настройки подключения к БД
    /// </summary>
    private const string DefaultConnectionName = "DefaultConnection";

    /// <summary>
    /// Возвращает строку подключения по имени.
    /// </summary>
    public static string GetConnectionStringOrThrow(this IConfiguration configuration, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        name ??= DefaultConnectionName;
        return configuration.GetConnectionString(name)
            ?? throw new InvalidOperationException($"Connection string '{name}' is not configured.");
    }
}