using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Platform.Core.Modules;

public static class ModuleRegistrationExtensions
{
    /// <summary>
    /// Регистрирует модуль и его сервисы в приложении.
    /// </summary>
    /// <typeparam name="TModule">
    /// Тип регистрируемого модуля.
    /// </typeparam>
    /// <param name="services">Коллекция сервисов приложения.</param>
    /// <param name="configuration">Конфигурация приложения.</param>
    /// <param name="environmentName">Имя окружения приложения.</param>
    /// <returns>Исходная коллекция сервисов для дальнейшей настройки.</returns>
    /// <exception cref="ArgumentNullException">
    /// Один из переданных аргументов равен <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Модуль уже зарегистрирован или его зависимости объявлены некорректно.
    /// </exception>
    /// <remarks>
    /// Исключения, возникшие во время выполнения RegisterServices,
    /// передаются вызывающему коду без подмены исходного исключения.
    /// При ошибке коллекция сервисов восстанавливается до состояния,
    /// существовавшего перед регистрацией модуля.
    /// </remarks>
    public static IServiceCollection AddModule<TModule>(this IServiceCollection services, IConfiguration configuration,
        string environmentName)
        where TModule : class, IModule, new()
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);

        var moduleType = typeof(TModule);

        EnsureNotRegistered<TModule>(services);

        var module = new TModule();
        var dependencies = GetDependencies(module);
        var context = new ModuleContext(services, configuration, environmentName);
        var originalDescriptors = services.ToArray();

        try
        {
            module.RegisterServices(context);
            services.AddSingleton<IModule>(module);
            services.AddSingleton(new ModuleRegistration(moduleType, dependencies));
        }
        catch
        {
            services.Clear();
            foreach (var descriptor in originalDescriptors)
            {
                services.Add(descriptor);
            }
            throw;
        }

        return services;
    }

    /// <summary>
    /// Проверяет наличие всех обязательных зависимостей
    /// зарегистрированных модулей.
    /// </summary>
    /// <param name="services">Коллекция сервисов приложения.</param>
    /// <returns>Исходная коллекция сервисов.</returns>
    /// <exception cref="ArgumentNullException">
    /// Коллекция сервисов равна <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Одна или несколько обязательных зависимостей не зарегистрированы
    /// либо обнаружена некорректная внутренняя запись модуля.
    /// </exception>
    /// <remarks>
    /// Метод не регистрирует отсутствующие модули и не проверяет
    /// все зависимости между обычными DI-сервисами.
    /// Рекомендуется вызывать его после регистрации всех модулей
    /// и до построения приложения.
    /// </remarks>
    public static IServiceCollection ValidateModules(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var registrations = services
            .Where(descriptor => descriptor.ServiceType == typeof(ModuleRegistration))
            .Select(descriptor => descriptor.ImplementationInstance as ModuleRegistration
                                  ?? throw new InvalidOperationException("Invalid module registration descriptor."))
            .ToArray();
        
        var duplicateModuleType = registrations
            .GroupBy(registration => registration.ModuleType)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateModuleType is not null)
        {
            throw new InvalidOperationException(
                $"Module '{duplicateModuleType.Key.FullName}' is registered more than once.");
        }
        
        var registeredTypes = registrations
            .Select(registration => registration.ModuleType)
            .ToHashSet();

        var errors = new List<string>();

        var cycles = FindDependencyCycles(registrations);

        foreach (var cycle in cycles)
        {
            errors.Add($"Circular module dependency detected: {cycle}.");
        }
        
        foreach (var registration in registrations)
        {
            foreach (var dependency in registration.Dependencies)
            {
                if (!registeredTypes.Contains(dependency))
                {
                    errors.Add(
                        $"Module '{registration.ModuleType.FullName}' " +
                        $"requires module '{dependency.FullName}', " +
                        "which has not been registered.");
                }
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException("Module dependency validation failed:" +
                                                Environment.NewLine +
                                                string.Join(Environment.NewLine, errors.Select(error => $"- {error}")));
        }

        return services;
    }

    private static void EnsureNotRegistered<TModule>(IServiceCollection services)
        where TModule : class, IModule
    {
        var moduleType = typeof(TModule);

        var alreadyRegistered = services.Any(descriptor =>
            descriptor.ServiceType == typeof(ModuleRegistration) &&
            descriptor.ImplementationInstance is ModuleRegistration registration &&
            registration.ModuleType == moduleType);

        if (alreadyRegistered)
        {
            throw new InvalidOperationException($"Module '{moduleType.FullName}' has already been registered.");
        }
    }

    private static IReadOnlyCollection<Type> GetDependencies(IModule module)
    {
        if (module is not IModuleDependencies dependencyProvider)
        {
            return Array.Empty<Type>();
        }

        var dependencies = dependencyProvider.Dependencies
                           ?? throw new InvalidOperationException($"Module '{module.GetType().FullName}' returned null dependencies.");

        var result = dependencies.ToArray();

        if (result.Any(type => type is null))
        {
            throw new InvalidOperationException($"Module '{module.GetType().FullName}' contains a null dependency.");
        }

        if (result.Any(type => !typeof(IModule).IsAssignableFrom(type)))
        {
            throw new InvalidOperationException($"Module '{module.GetType().FullName}' declares a dependency " +
                                                "that does not implement IModule.");
        }

        if (result.Contains(module.GetType()))
        {
            throw new InvalidOperationException($"Module '{module.GetType().FullName}' cannot depend on itself.");
        }

        if (result.Distinct().Count() != result.Length)
        {
            throw new InvalidOperationException($"Module '{module.GetType().FullName}' contains duplicate dependencies.");
        }

        return Array.AsReadOnly(result);
    }

    /// <summary>
    /// Находит циклические зависимости между зарегистрированными модулями.
    /// </summary>
    /// <param name="registrations">
    /// Коллекция регистраций модулей, зависимости которых необходимо проверить.
    /// </param>
    /// <returns>
    /// Список описаний обнаруженных циклов. Каждый цикл представлен
    /// последовательностью имён модулей, в которой последний модуль
    /// совпадает с первым.
    /// </returns>
    /// <remarks>
    /// При построении графа учитываются только зависимости, модули которых
    /// зарегистрированы в переданной коллекции. Отсутствующие зависимости
    /// должны проверяться отдельно.
    /// Каждый цикл добавляется в результат не более одного раза.
    /// </remarks>
    private static IReadOnlyList<string> FindDependencyCycles(IReadOnlyCollection<ModuleRegistration> registrations)
    {
        var orderedTypes = registrations
        .Select(registration => registration.ModuleType)
        .OrderBy(GetTypeKey, StringComparer.Ordinal)
        .ToArray();

    var indexByType = orderedTypes
        .Select((type, index) => (type, index))
        .ToDictionary(item => item.type, item => item.index);

    var dependencies = registrations.ToDictionary(
        registration => registration.ModuleType,
        registration => registration.Dependencies
            .Where(indexByType.ContainsKey)
            .Distinct()
            .OrderBy(dependency => indexByType[dependency])
            .ToArray());

    var cycles = new List<string>();
    var uniqueCycles = new HashSet<string>(StringComparer.Ordinal);

    for (var startIndex = 0; startIndex < orderedTypes.Length; startIndex++)
    {
        var start = orderedTypes[startIndex];
        var path = new List<Type> { start };
        var active = new HashSet<Type> { start };

        void Visit(Type current)
        {
            foreach (var dependency in dependencies[current])
            {
                var dependencyIndex = indexByType[dependency];

                // The start node must be the lowest-indexed node in a cycle.
                // This ensures that each cycle is discovered only once
                // regardless of its starting point.
                if (dependencyIndex < startIndex)
                {
                    continue;
                }

                if (dependency == start)
                {
                    var cycleKey = string.Join(
                        " -> ",
                        path.Select(GetTypeKey));

                    if (uniqueCycles.Add(cycleKey))
                    {
                        cycles.Add(
                            string.Join(
                                " -> ",
                                path.Select(type => type.Name)
                                    .Append(start.Name)));
                    }

                    continue;
                }

                if (!active.Add(dependency))
                {
                    continue;
                }

                path.Add(dependency);
                Visit(dependency);
                path.RemoveAt(path.Count - 1);
                active.Remove(dependency);
            }
        }

        Visit(start);
    }

    return cycles;
    }
    
    private static string GetTypeKey(Type type) =>
        type.AssemblyQualifiedName
        ?? type.FullName
        ?? type.Name;
}