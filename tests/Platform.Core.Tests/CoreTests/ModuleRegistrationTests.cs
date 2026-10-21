using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Core.Modules;

namespace Platform.Core.Tests.CoreTests;

public sealed partial class ModuleRegistrationTests
{
    private readonly IServiceCollection _services = new ServiceCollection();

    private readonly IConfiguration _configuration = new ConfigurationBuilder().Build();

    private const string EnvironmentName = "Testing";

    [Fact]
    public void AddModule_RegistersModule()
    {
        _services.AddModule<TestModule>(
            _configuration,
            EnvironmentName);

        Assert.Single(_services, d => d.ServiceType == typeof(IModule));
    }

    [Fact]
    public void AddModule_PassesContextToModule()
    {
        TestModule.Reset();

        _services.AddModule<TestModule>(
            _configuration,
            EnvironmentName);

        Assert.Same(_services, TestModule.Context!.Services);
        Assert.Same(_configuration, TestModule.Context.Configuration);
        Assert.Equal(EnvironmentName, TestModule.Context.EnvironmentName);
    }

    [Fact]
    public void AddModule_DuplicateRegistration_Throws()
    {
        _services.AddModule<TestModule>(
            _configuration,
            EnvironmentName);

        Assert.Throws<InvalidOperationException>(() =>
            _services.AddModule<TestModule>(
                _configuration,
                EnvironmentName));
    }

    [Fact]
    public void ValidateModules_MissingDependency_Throws()
    {
        _services.AddModule<DependentModule>(
            _configuration,
            EnvironmentName);

        var exception = Assert.Throws<InvalidOperationException>(
            () => _services.ValidateModules());

        Assert.Contains(
            nameof(RequiredModule),
            exception.Message);
    }

    [Fact]
    public void ValidateModules_RegisteredDependency_Succeeds()
    {
        _services.AddModule<DependentModule>(
            _configuration,
            EnvironmentName);

        _services.AddModule<RequiredModule>(
            _configuration,
            EnvironmentName);

        var result = _services.ValidateModules();

        Assert.Same(_services, result);
    }

    public sealed class TestModule : IModule
    {
        public static ModuleContext? Context { get; private set; }

        public static void Reset() => Context = null;

        public void RegisterServices(ModuleContext context)
        {
            Context = context;
        }
    }

    [Fact(DisplayName = "Регистрация модуля должна передавать исключение, возникшее в RegisterServices")]
    public void AddModule_WhenRegisterServicesThrows_PropagatesException()
    {
        var thrownException = Assert.Throws<InvalidOperationException>(
            () => _services.AddModule<FailingModule>(
                _configuration,
                EnvironmentName));

        Assert.Equal("Ошибка регистрации тестового модуля", thrownException.Message);
        Assert.IsType<InvalidOperationException>(thrownException);
    }
    
    [Fact(DisplayName = "При ошибке регистрации модуля изменения в коллекции сервисов должны быть отменены")]
    public void AddModule_WhenRegisterServicesThrows_RestoresServices()
    {
        var existingDescriptor =
            ServiceDescriptor.Singleton<IExistingService, ExistingService>();

        _services.Add(existingDescriptor);

        var exception = Assert.Throws<InvalidOperationException>(
            () => _services.AddModule<FailingModuleWithService>(
                _configuration,
                EnvironmentName));

        Assert.Same(FailingModuleWithService.Exception, exception);

        Assert.Contains(existingDescriptor, _services);

        Assert.DoesNotContain(
            _services,
            descriptor => descriptor.ServiceType == typeof(IFailingService));

        Assert.DoesNotContain(
            _services,
            descriptor => descriptor.ServiceType == typeof(IModule));

        Assert.DoesNotContain(
            _services,
            descriptor => descriptor.ServiceType == typeof(ModuleRegistration));
    }
    
    [Fact(DisplayName = "Модуль не должен зависеть от самого себя")]
    public void AddModule_WhenModuleDependsOnItself_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => _services.AddModule<SelfDependentModule>(
                _configuration,
                EnvironmentName));

        Assert.Contains(
            "cannot depend on itself",
            exception.Message);
    }

    [Fact(DisplayName = "Зависимость модуля должна реализовывать IModule")]
    public void AddModule_WhenDependencyIsNotModule_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => _services.AddModule<InvalidDependencyModule>(
                _configuration,
                EnvironmentName));

        Assert.Contains(
            "does not implement IModule",
            exception.Message);
    }

    [Fact(DisplayName = "Список зависимостей не должен содержать повторяющиеся типы")]
    public void AddModule_WhenDependenciesContainDuplicates_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => _services.AddModule<DuplicateDependencyModule>(
                _configuration,
                EnvironmentName));

        Assert.Contains(
            "duplicate dependencies",
            exception.Message);
    }
    
    [Fact(DisplayName = "Модуль без обязательных зависимостей должен проходить проверку")]
    public void ValidateModules_WhenModuleHasNoDependencies_Succeeds()
    {
        _services.AddModule<TestModule>(
            _configuration,
            EnvironmentName);

        var result = _services.ValidateModules();

        Assert.Same(_services, result);
    }
    
    [Fact(DisplayName = "Проверка должна сообщать обо всех отсутствующих зависимостях")]
    public void ValidateModules_WhenMultipleDependenciesAreMissing_ReportsAllDependencies()
    {
        _services.AddModule<MultipleDependenciesModule>(
            _configuration,
            EnvironmentName);

        var exception = Assert.Throws<InvalidOperationException>(
            () => _services.ValidateModules());

        Assert.Contains(nameof(MissingModuleA), exception.Message);
        Assert.Contains(nameof(MissingModuleB), exception.Message);
    }
    
    [Fact(DisplayName = "Проверка зависимостей не должна изменять коллекцию сервисов")]
    public void ValidateModules_DoesNotModifyServiceCollection()
    {
        _services.AddModule<DependentModule>(
            _configuration,
            EnvironmentName);

        var originalDescriptors = _services.ToArray();

        Assert.Throws<InvalidOperationException>(
            () => _services.ValidateModules());

        Assert.Equal(originalDescriptors.Length, _services.Count);

        for (var index = 0; index < originalDescriptors.Length; index++)
        {
            Assert.Same(originalDescriptors[index], _services[index]);
        }
    }
    
    public sealed class RequiredModule : IModule
    {
        public void RegisterServices(ModuleContext context)
        {
        }
    }

    public sealed class DependentModule : IModule, IModuleDependencies
    {
        public IReadOnlyCollection<Type> Dependencies =>
            [typeof(RequiredModule)];

        public void RegisterServices(ModuleContext context)
        {
        }
    }
    
    public sealed class FailingModule : IModule
    {
        public static InvalidOperationException Exception { get; } =
            new("Ошибка регистрации тестового модуля");

        public void RegisterServices(ModuleContext context)
        {
            throw Exception;
        }
    }
    
    public interface IExistingService;

    public sealed class ExistingService : IExistingService;

    public interface IFailingService;

    public sealed class FailingService : IFailingService;

    public sealed class FailingModuleWithService : IModule
    {
        public static InvalidOperationException Exception { get; } =
            new("Ошибка регистрации тестового модуля");

        public void RegisterServices(ModuleContext context)
        {
            context.Services.AddSingleton<IFailingService, FailingService>();

            throw Exception;
        }
    }
    
    public sealed class SelfDependentModule : IModule, IModuleDependencies
    {
        public IReadOnlyCollection<Type> Dependencies =>
            [typeof(SelfDependentModule)];

        public void RegisterServices(ModuleContext context)
        {
        }
    }

    public sealed class InvalidDependencyModule : IModule, IModuleDependencies
    {
        public IReadOnlyCollection<Type> Dependencies =>
            [typeof(string)];

        public void RegisterServices(ModuleContext context)
        {
        }
    }

    public sealed class DuplicateDependencyModule : IModule, IModuleDependencies
    {
        public IReadOnlyCollection<Type> Dependencies =>
            [typeof(RequiredModule), typeof(RequiredModule)];

        public void RegisterServices(ModuleContext context)
        {
        }
    }
    
    public sealed class MissingModuleA : IModule
    {
        public void RegisterServices(ModuleContext context)
        {
        }
    }

    public sealed class MissingModuleB : IModule
    {
        public void RegisterServices(ModuleContext context)
        {
        }
    }

    public sealed class MultipleDependenciesModule
        : IModule, IModuleDependencies
    {
        public IReadOnlyCollection<Type> Dependencies =>
            [typeof(MissingModuleA), typeof(MissingModuleB)];

        public void RegisterServices(ModuleContext context)
        {
        }
    }
}