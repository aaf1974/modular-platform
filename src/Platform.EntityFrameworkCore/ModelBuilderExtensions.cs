using Microsoft.EntityFrameworkCore;
using Platform.Core.Modules;

namespace Platform.EntityFrameworkCore;

/// <summary>
/// Содержит методы расширения для конфигурирования общей модели EF Core.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Применяет конфигурации сущностей из явно указанных сборок.
    /// </summary>
    /// <param name="modelBuilder">Построитель модели EF Core.</param>
    /// <param name="assemblies">
    /// Сборки, содержащие реализации
    /// <see cref="IEntityTypeConfiguration{TEntity}"/>.
    /// </param>
    /// <returns>Исходный построитель модели.</returns>
    /// <exception cref="ArgumentNullException">
    /// Один из аргументов равен <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Массив сборок пуст или содержит элемент, равный
    /// <see langword="null"/>.
    /// </exception>
    public static ModelBuilder ApplyModuleConfigurations(this ModelBuilder modelBuilder, params System.Reflection.Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentNullException.ThrowIfNull(assemblies);

        if (assemblies.Length == 0)
        {
            throw new ArgumentException(
                "Необходимо указать хотя бы одну сборку с конфигурациями EF Core.",
                nameof(assemblies));
        }

        if (assemblies.Any(assembly => assembly is null))
        {
            throw new ArgumentException(
                "Список сборок не должен содержать null.",
                nameof(assemblies));
        }

        foreach (var assembly in assemblies.Distinct())
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }

        return modelBuilder;
    }
    
    /// <summary>
    /// Применяет конфигурации сущностей всех зарегистрированных модулей,
    /// реализующих <see cref="IEfModule"/>.
    /// </summary>
    /// <param name="modelBuilder">Построитель модели EF Core.</param>
    /// <param name="modules">Зарегистрированные экземпляры модулей.</param>
    /// <returns>Исходный построитель модели.</returns>
    /// <exception cref="ArgumentNullException">
    /// Один из аргументов равен <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Модуль не предоставил допустимый список сборок.
    /// </exception>
    public static ModelBuilder ApplyModuleConfigurations(this ModelBuilder modelBuilder, IEnumerable<IModule> modules)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentNullException.ThrowIfNull(modules);

        var assemblies = new List<System.Reflection.Assembly>();

        foreach (var module in modules)
        {
            if (module is null)
            {
                throw new InvalidOperationException(
                    "Список модулей не должен содержать null.");
            }

            if (module is not IEfModule efModule)
            {
                continue;
            }

            var configurationAssemblies = efModule.ConfigurationAssemblies
                ?? throw new InvalidOperationException(
                    $"Модуль '{module.GetType().FullName}' " +
                    "вернул null вместо списка EF Core-сборок.");

            foreach (var assembly in configurationAssemblies)
            {
                if (assembly is null)
                {
                    throw new InvalidOperationException(
                        $"Модуль '{module.GetType().FullName}' " +
                        "содержит null в списке EF Core-сборок.");
                }

                assemblies.Add(assembly);
            }
        }

        var distinctAssemblies = assemblies.Distinct().ToArray();

        if (distinctAssemblies.Length == 0)
        {
            return modelBuilder;
        }

        return modelBuilder.ApplyModuleConfigurations(distinctAssemblies);
    }
}