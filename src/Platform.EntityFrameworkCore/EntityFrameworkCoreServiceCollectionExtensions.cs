using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Platform.EntityFrameworkCore;

/// <summary>
/// Методы регистрации инфраструктуры Entity Framework Core платформы.
/// </summary>
public static class EntityFrameworkCoreServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует общий <see cref="ApplicationDbContext"/> приложения и фабрику его создания.
    /// Контекст доступен как scoped-сервис и через <see cref="IDbContextFactory{TContext}"/>.
    /// </summary>
    /// <param name="services">Коллекция сервисов приложения.</param>
    /// <param name="configureOptions">
    /// Настройка EF Core. Приложение выбирает провайдер, например PostgreSQL через Npgsql.
    /// </param>
    /// <returns>Исходная коллекция сервисов.</returns>
    /// <exception cref="ArgumentNullException">
    /// Один из аргументов равен <see langword="null"/>.
    /// </exception>
    public static IServiceCollection AddPlatformEntityFrameworkCore(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddDbContextFactory<ApplicationDbContext>(options =>
        {
            configureOptions(options);
            options.ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory,
                ApplicationModelCacheKeyFactory>();
        });
        return services;
    }
}
