using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Platform.AspNetCore.Configuration;
using Platform.AspNetCore.Modules;

namespace Platform.AspNetCore.Tests;

public sealed class PlatformWebFeatureTests
{
    [Fact(DisplayName = "UseModules configures registered platform web features")]
    public void UseModules_ShouldConfigureRegisteredFeatures()
    {
        // Arrange
        var feature = new TestPlatformWebFeature();

        var builder = WebApplication.CreateBuilder();

        builder.Services.AddPlatform(options =>
            options.AddWebFeature(feature));

        builder.AddModule<WebModuleTests.TestWebModule>();

        using var app = builder.Build();

        // Act
        app.UseModules();

        // Assert
        feature.WasConfiguredApplication.ShouldBeTrue();
        feature.WasConfiguredServices.ShouldBeTrue();
    }

    [Fact(DisplayName = "Проверяет, что повторная регистрация одного экземпляра возможности отклоняется.")]
    public void AddPlatform_ShouldRejectDuplicateFeatureInstancesInOneConfiguration()
    {
        // Arrange
        var feature = new TestPlatformWebFeature();
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddPlatform(options =>
            options
                .AddWebFeature(feature)
                .AddWebFeature(feature));

        // Assert
        Should.Throw<InvalidOperationException>(act);
        feature.WasConfiguredServices.ShouldBeFalse();
        services.ShouldNotContain(descriptor => descriptor.ServiceType == typeof(IPlatformWebFeature));
    }

    [Fact(DisplayName = "Проверяет, что повторная регистрация экземпляра через другой вызов AddPlatform отклоняется.")]
    public void AddPlatform_ShouldRejectFeatureInstanceRegisteredByPreviousCall()
    {
        // Arrange
        var feature = new TestPlatformWebFeature();
        var services = new ServiceCollection();

        services.AddPlatform(options => options.AddWebFeature(feature));

        // Act
        var act = () => services.AddPlatform(options =>
            options.AddWebFeature(feature));

        // Assert
        Should.Throw<InvalidOperationException>(act);
        feature.WasConfiguredServices.ShouldBeTrue();
        services.Count(descriptor => descriptor.ServiceType == typeof(IPlatformWebFeature))
            .ShouldBe(1);
    }

    [Fact(DisplayName = "Проверяет, что разные экземпляры одной реализации можно зарегистрировать одновременно.")]
    public void AddPlatform_ShouldAllowDistinctInstancesOfSameFeatureType()
    {
        // Arrange
        var firstFeature = new TestPlatformWebFeature();
        var secondFeature = new TestPlatformWebFeature();
        var services = new ServiceCollection();

        // Act
        services.AddPlatform(options =>
            options
                .AddWebFeature(firstFeature)
                .AddWebFeature(secondFeature));

        // Assert
        firstFeature.WasConfiguredServices.ShouldBeTrue();
        secondFeature.WasConfiguredServices.ShouldBeTrue();

        services.Count(descriptor => descriptor.ServiceType == typeof(IPlatformWebFeature))
            .ShouldBe(2);
    }

    [Fact(DisplayName = "Проверяет, что инфраструктурные возможности применяются в порядке регистрации и каждая возможность настраивает приложение один раз")]
    public void UseModules_ShouldConfigureFeaturesInRegistrationOrderExactlyOnce()
    {
        // Arrange
        var executionOrder = new List<string>();
        var firstFeature = new RecordingPlatformWebFeature("First", executionOrder);
        var secondFeature = new RecordingPlatformWebFeature("Second", executionOrder);
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddPlatform(options =>
            options
                .AddWebFeature(firstFeature)
                .AddWebFeature(secondFeature));

        using var app = builder.Build();

        // Act
        app.UseModules();

        // Assert
        executionOrder.ShouldBe(["First", "Second"]);
        firstFeature.ApplicationConfigurationCount.ShouldBe(1);
        secondFeature.ApplicationConfigurationCount.ShouldBe(1);
    }

    [Fact(DisplayName = "Проверяет, что после ошибки настройки возможности повторный вызов UseModules запрещён")]
    public void UseModules_ShouldRejectRetryWhenFeatureConfigurationThrows()
    {
        // Arrange
        var executionOrder = new List<string>();
        var failingFeature = new RecordingPlatformWebFeature(
            "Failing",
            executionOrder,
            shouldThrow: true);

        var builder = WebApplication.CreateBuilder();

        builder.Services.AddPlatform(options =>
            options.AddWebFeature(failingFeature));

        using var app = builder.Build();

        // Act
        var firstAct = () => app.UseModules();

        // Assert
        Should.Throw<InvalidOperationException>(firstAct)
            .Message.ShouldBe("Ошибка настройки тестовой возможности.");

        failingFeature.ApplicationConfigurationCount.ShouldBe(1);
        executionOrder.ShouldBe(["Failing"]);

        Should.Throw<InvalidOperationException>(() => app.UseModules())
            .Message.ShouldBe(
                "Метод UseModules() уже вызывался для данного веб-приложения.");

        failingFeature.ApplicationConfigurationCount.ShouldBe(1);
        executionOrder.ShouldBe(["Failing"]);
    }

    private sealed class TestPlatformWebFeature : IPlatformWebFeature
    {
        public bool WasConfiguredServices { get; private set; }

        public bool WasConfiguredApplication { get; private set; }

        public void ConfigureServices(IServiceCollection services)
        {
            WasConfiguredServices = true;
        }

        public void ConfigureApplication(WebApplication app)
        {
            ArgumentNullException.ThrowIfNull(app);

            WasConfiguredApplication = true;
        }
    }

    /// <summary>
    /// Представляет тестовую возможность, записывающую порядок и количество вызовов настройки.
    /// </summary>
    private sealed class RecordingPlatformWebFeature : IPlatformWebFeature
    {
        private readonly string _name;
        private readonly List<string> _executionOrder;
        private readonly bool _shouldThrow;

        /// <summary>
        /// Создаёт тестовую возможность с заданным именем и журналом вызовов.
        /// </summary>
        /// <param name="name">Имя возможности для записи в журнал.</param>
        /// <param name="executionOrder">Общий журнал порядка выполнения.</param>
        /// <param name="shouldThrow">Признак необходимости выбросить исключение при настройке приложения.</param>
        public RecordingPlatformWebFeature(
            string name,
            List<string> executionOrder,
            bool shouldThrow = false)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(executionOrder);

            _name = name;
            _executionOrder = executionOrder;
            _shouldThrow = shouldThrow;
        }

        /// <summary>
        /// Получает количество вызовов настройки приложения.
        /// </summary>
        public int ApplicationConfigurationCount { get; private set; }

        /// <summary>
        /// Выполняет регистрацию сервисов тестовой возможности.
        /// </summary>
        /// <param name="services">Коллекция сервисов приложения.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
        }

        /// <summary>
        /// Записывает вызов настройки приложения и при необходимости выбрасывает исключение.
        /// </summary>
        /// <param name="app">Построенное веб-приложение.</param>
        public void ConfigureApplication(WebApplication app)
        {
            ArgumentNullException.ThrowIfNull(app);

            ApplicationConfigurationCount++;
            _executionOrder.Add(_name);

            if (_shouldThrow)
            {
                throw new InvalidOperationException(
                    "Ошибка настройки тестовой возможности.");
            }
        }
    }
}