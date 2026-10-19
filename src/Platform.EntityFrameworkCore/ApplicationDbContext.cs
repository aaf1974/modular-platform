using Microsoft.EntityFrameworkCore;
using Platform.Core.Modules;

namespace Platform.EntityFrameworkCore;

/// <summary>
/// Общий контекст базы данных приложения для сущностей всех подключённых модулей.
/// </summary>
/// <remarks>
/// Модули явно регистрируются приложением через механизм Platform.Core.
/// Модули, реализующие <see cref="IEfModule"/>, предоставляют сборки с
/// конфигурациями сущностей; эти конфигурации объединяются в единую модель.
/// Выбор провайдера базы данных и строки подключения остаётся ответственностью приложения.
/// </remarks>
public class ApplicationDbContext : DbContext
{
    private readonly IReadOnlyCollection<IModule> _modules;

    /// <summary>
    /// Ключ набора модулей, влияющего на построение EF-модели.
    /// Используется фабрикой ключей кэша модели.
    /// </summary>
    internal string ModelCacheKey { get; }

    /// <summary>
    /// Создаёт общий контекст приложения.
    /// </summary>
    /// <param name="options">Настройки контекста.</param>
    /// <param name="modules">Экземпляры явно зарегистрированных модулей.</param>
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IEnumerable<IModule> modules)
        : base(options)
    {
        ArgumentNullException.ThrowIfNull(modules);
        _modules = modules.ToArray();
        ModelCacheKey = CreateModelCacheKey(_modules);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyModuleConfigurations(_modules);
    }

    private static string CreateModelCacheKey(IEnumerable<IModule> modules)
    {
        var moduleKeys = new List<string>();

        foreach (var module in modules)
        {
            var moduleKey = module.GetType().AssemblyQualifiedName
                ?? module.GetType().FullName
                ?? module.GetType().Name;

            if (module is IEfModule efModule)
            {
                var assemblies = efModule.ConfigurationAssemblies
                    ?? throw new InvalidOperationException(
                        $"Модуль '{module.GetType().FullName}' " +
                        "вернул null вместо списка EF Core-сборок.");

                var assemblyNames = assemblies.Select(assembly =>
                    assembly?.FullName ?? throw new InvalidOperationException(
                        $"Модуль '{module.GetType().FullName}' " +
                        "содержит null в списке EF Core-сборок."));

                moduleKey += "[" + string.Join(";", assemblyNames) + "]";
            }

            moduleKeys.Add(moduleKey);
        }

        return string.Join("|", moduleKeys);
    }
}
