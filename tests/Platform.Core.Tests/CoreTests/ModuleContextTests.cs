using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Core.Modules;

namespace Platform.Core.Tests.CoreTests;

public class ModuleContextTests
{
    [Theory(DisplayName = "Имя окружения не должно быть пустым или состоять из пробелов")]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhitespaceEnvironmentName_Throws(string environmentName)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        Assert.Throws<ArgumentException>(() =>
            new ModuleContext(
                services,
                configuration,
                environmentName));
    }
}