using Microsoft.Extensions.Configuration;

namespace Platform.Core.Tests.ExtensionsTest;

public class ConnectionStringTests
{
    [Fact(DisplayName = "Использует имя DefaultConnection, если имя не указано")]
    public void GetConnectionStringOrThrow_UsesDefaultName_WhenNameIsOmitted()
    {
        var configuration = CreateConfiguration(
            ("ConnectionStrings:DefaultConnection", "Server=localhost;"));

        var result = configuration.GetConnectionStringOrThrow();

        result.ShouldBe("Server=localhost;");
    }

    [Fact(DisplayName = "Использует имя DefaultConnection, если передан null")]
    public void GetConnectionStringOrThrow_UsesDefaultName_WhenNameIsNull()
    {
        var configuration = CreateConfiguration(
            ("ConnectionStrings:DefaultConnection", "Server=localhost;"));

        var result = configuration.GetConnectionStringOrThrow(null);

        result.ShouldBe("Server=localhost;");
    }

    [Fact(DisplayName = "Возвращает строку подключения для указанного имени")]
    public void GetConnectionStringOrThrow_UsesCustomName()
    {
        var configuration = CreateConfiguration(
            ("ConnectionStrings:Reporting", "Server=reporting;"));

        var result = configuration.GetConnectionStringOrThrow("Reporting");

        result.ShouldBe("Server=reporting;");
    }

    [Fact(DisplayName = "Выбрасывает исключение, если строка DefaultConnection не настроена")]
    public void GetConnectionStringOrThrow_Throws_WhenConnectionStringIsMissing()
    {
        var configuration = CreateConfiguration();

        var exception = Should.Throw<InvalidOperationException>(() => configuration.GetConnectionStringOrThrow());

        exception.Message.ShouldBe(
            "Connection string 'DefaultConnection' is not configured.");
    }

    [Fact(DisplayName = "Выбрасывает исключение, если строка подключения с указанным именем не настроена")]
    public void GetConnectionStringOrThrow_Throws_WhenCustomConnectionStringIsMissing()
    {
        var configuration = CreateConfiguration();

        var exception =
            Should.Throw<InvalidOperationException>(() => configuration.GetConnectionStringOrThrow("Reporting"));

        exception.Message.ShouldBe(
            "Connection string 'Reporting' is not configured.");
    }

    [Fact(DisplayName = "Выбрасывает ArgumentNullException, если конфигурация равна null")]
    public void GetConnectionStringOrThrow_ThrowsArgumentNullException_WhenConfigurationIsNull()
    {
        IConfiguration configuration = null!;

        Should.Throw<ArgumentNullException>(() => configuration.GetConnectionStringOrThrow());
    }
    
    
    private static IConfiguration CreateConfiguration(params (string Key, string Value)[] settings)
    {
        var values = settings.ToDictionary(
            setting => setting.Key,
            setting => (string?)setting.Value);

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}