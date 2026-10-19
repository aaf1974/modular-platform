using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Platform.AspNetCore.Modules;
using Platform.Core.Modules;

namespace Platform.AspNetCore.Tests;

/// <summary>
/// Тестовый модуль, предоставляющий HTTP endpoint.
/// </summary>
public sealed class TestEndpointModule : IModule, IWebModule
{
    /// <inheritdoc />
    public void RegisterServices(ModuleContext context)
    {
        // В этом сценарии дополнительные сервисы не требуются.
    }

    /// <inheritdoc />
    public void ConfigureApplication(WebModuleContext context)
    {
        context.Application.MapGet(
            "/api/business/health",
            () => Results.Ok(new
            {
                Module = "Business",
                Status = "Available"
            }));
    }
}