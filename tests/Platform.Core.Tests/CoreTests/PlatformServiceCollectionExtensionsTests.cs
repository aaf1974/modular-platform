using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Platform.AspNetCore.Configuration;
using Platform.AspNetCore.Exceptions;

namespace Platform.AspNetCore.Tests.Configuration;

public sealed class PlatformServiceCollectionExtensionsTests
{
    [Fact(DisplayName = "AddPlatform registers the default exception handler")]
    public void AddPlatform_ShouldRegisterDefaultExceptionHandler()
    {
        var services = new ServiceCollection();

        services.AddPlatform(_ => { });

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IExceptionHandler) &&
                descriptor.ImplementationType ==
                typeof(DefaultExceptionHandler));
    }
}