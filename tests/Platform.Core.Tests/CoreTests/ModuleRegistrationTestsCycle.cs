using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Platform.Core.Modules;

namespace Platform.Core.Tests.CoreTests;

public partial class ModuleRegistrationTests
{
    [Fact(DisplayName = "Проверка зависимостей успешно проходит для пустой коллекции сервисов")]
    public void ValidateModules_EmptyCollection_Succeeds()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.ValidateModules();

        // Assert
        Assert.Same(services, result);
    }

    [Fact(DisplayName = "Проверка обнаруживает циклическую зависимость между двумя модулями")]
    public void ValidateModules_CircularDependency_Throws()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddModule<CycleAModule>(configuration, Environments.Development);
        services.AddModule<CycleBModule>(configuration, Environments.Development);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => services.ValidateModules());

        // Assert
        Assert.Contains(nameof(CycleAModule), exception.Message);
        Assert.Contains(nameof(CycleBModule), exception.Message);
    }

    [Fact(DisplayName = "Проверка обнаруживает циклическую зависимость между тремя модулями")]
    public void ValidateModules_ThreeModuleCycle_Throws()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddModule<CycleThreeAModule>(configuration, Environments.Development);
        services.AddModule<CycleThreeBModule>(configuration, Environments.Development);
        services.AddModule<CycleThreeCModule>(configuration, Environments.Development);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => services.ValidateModules());

        // Assert
        Assert.Contains(nameof(CycleThreeAModule), exception.Message);
        Assert.Contains(nameof(CycleThreeBModule), exception.Message);
        Assert.Contains(nameof(CycleThreeCModule), exception.Message);
    }

    [Fact(DisplayName = "Проверка зависимостей не вызывает повторную регистрацию сервисов модуля")]
    public void ValidateModules_DoesNotCallRegisterServicesAgain()
    {
        // Arrange
        CountingModule.RegistrationCount = 0;

        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddModule<CountingModule>(configuration, Environments.Development);
        Assert.Equal(1, CountingModule.RegistrationCount);

        // Act
        services.ValidateModules();

        // Assert
        Assert.Equal(1, CountingModule.RegistrationCount);
    }

    [Fact(DisplayName =
        "Проверка должна обнаруживать два пересекающихся цикла как отдельные циклы")]
    public void ValidateModules_OverlappingCycles_ReportsBothCycles()
    {
        var services = new ServiceCollection();

        services.AddSingleton(new ModuleRegistration(
            typeof(ModuleA),
            new[] { typeof(ModuleB) }));

        services.AddSingleton(new ModuleRegistration(
            typeof(ModuleB),
            new[] { typeof(ModuleA), typeof(ModuleC) }));

        services.AddSingleton(new ModuleRegistration(
            typeof(ModuleC),
            new[] { typeof(ModuleA) }));

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.ValidateModules());

        Assert.Contains("Circular module dependency", exception.Message);
        Assert.Contains(nameof(ModuleA), exception.Message);
        Assert.Contains(nameof(ModuleB), exception.Message);
        Assert.Contains(nameof(ModuleC), exception.Message);

        Assert.Equal(
            2,
            CountOccurrences(
                exception.Message,
                "Circular module dependency detected"));
    }

    [Fact(DisplayName =
        "Проверка должна обнаруживать несколько независимых циклов")]
    public void ValidateModules_IndependentCycles_ReportsBothCycles()
    {
        var services = new ServiceCollection();

        services.AddSingleton(new ModuleRegistration(
            typeof(ModuleA),
            new[] { typeof(ModuleB) }));

        services.AddSingleton(new ModuleRegistration(
            typeof(ModuleB),
            new[] { typeof(ModuleA) }));

        services.AddSingleton(new ModuleRegistration(
            typeof(ModuleC),
            new[] { typeof(ModuleD) }));

        services.AddSingleton(new ModuleRegistration(
            typeof(ModuleD),
            new[] { typeof(ModuleC) }));

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.ValidateModules());

        Assert.Equal(
            2,
            CountOccurrences(
                exception.Message,
                "Circular module dependency detected"));
    }

    [Fact(DisplayName =
        "Проверка должна сообщать об отсутствующих зависимостях и циклах одновременно")]
    public void ValidateModules_MissingDependencyAndCycle_ReportsBothErrors()
    {
        var services = new ServiceCollection();

        services.AddSingleton(new ModuleRegistration(
            typeof(ModuleA),
            new[] { typeof(ModuleB), typeof(ModuleMissing) }));

        services.AddSingleton(new ModuleRegistration(
            typeof(ModuleB),
            new[] { typeof(ModuleA) }));

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.ValidateModules());

        Assert.Contains("Circular module dependency detected", exception.Message);
        Assert.Contains(nameof(ModuleMissing), exception.Message);
        Assert.Contains("has not been registered", exception.Message);
    }

    [Fact(DisplayName =
        "Повторная внутренняя регистрация модуля должна приводить к понятной ошибке")]
    public void ValidateModules_DuplicateInternalRegistration_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();

        services.AddSingleton(new ModuleRegistration(
            typeof(ModuleA),
            Array.Empty<Type>()));

        services.AddSingleton(new ModuleRegistration(
            typeof(ModuleA),
            Array.Empty<Type>()));

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.ValidateModules());

        Assert.Contains(nameof(ModuleA), exception.Message);
        Assert.Contains("registered more than once", exception.Message);
    }

    [Fact(DisplayName =
        "При ошибке регистрации модуля коллекция сервисов должна восстанавливаться")]
    public void AddModule_RegistrationFailure_RestoresOriginalServices()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddSingleton<TestService>();

        var originalDescriptors = services.ToArray();

        Assert.Throws<TestRegistrationException>(() =>
            services.AddModule<ThrowingModule>(
                configuration,
                "Test"));

        Assert.Equal(originalDescriptors, services.ToArray());
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;

        while ((index = source.IndexOf(
            value,
            index,
            StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private sealed class ModuleA : IModule
    {
        public void RegisterServices(ModuleContext context) { }
    }

    private sealed class ModuleB : IModule
    {
        public void RegisterServices(ModuleContext context) { }
    }

    private sealed class ModuleC : IModule
    {
        public void RegisterServices(ModuleContext context) { }
    }

    private sealed class ModuleD : IModule
    {
        public void RegisterServices(ModuleContext context) { }
    }

    private sealed class ModuleMissing : IModule
    {
        public void RegisterServices(ModuleContext context) { }
    }

    private sealed class TestService;

    private sealed class TestRegistrationException : Exception;

    private sealed class ThrowingModule : IModule
    {
        public void RegisterServices(ModuleContext context)
        {
            context.Services.AddSingleton<AnotherTestService>();

            throw new TestRegistrationException();
        }
    }

    private sealed class AnotherTestService;
    
    private sealed class CycleAModule : IModule, IModuleDependencies
    {
        public IReadOnlyCollection<Type> Dependencies =>
            [typeof(CycleBModule)];

        public void RegisterServices(ModuleContext context)
        {
        }
    }

    private sealed class CycleBModule : IModule, IModuleDependencies
    {
        public IReadOnlyCollection<Type> Dependencies =>
            [typeof(CycleAModule)];

        public void RegisterServices(ModuleContext context)
        {
        }
    }

    private sealed class CycleThreeAModule : IModule, IModuleDependencies
    {
        public IReadOnlyCollection<Type> Dependencies =>
            [typeof(CycleThreeBModule)];

        public void RegisterServices(ModuleContext context)
        {
        }
    }

    private sealed class CycleThreeBModule : IModule, IModuleDependencies
    {
        public IReadOnlyCollection<Type> Dependencies =>
            [typeof(CycleThreeCModule)];

        public void RegisterServices(ModuleContext context)
        {
        }
    }

    private sealed class CycleThreeCModule : IModule, IModuleDependencies
    {
        public IReadOnlyCollection<Type> Dependencies =>
            [typeof(CycleThreeAModule)];

        public void RegisterServices(ModuleContext context)
        {
        }
    }

    private sealed class CountingModule : IModule
    {
        public static int RegistrationCount { get; set; }

        public void RegisterServices(ModuleContext context)
        {
            RegistrationCount++;
        }
    }
}