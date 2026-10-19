using System.Reflection;
using Business.Services;
using Microsoft.Extensions.DependencyInjection;
using Platform.Core.Modules;
using Platform.EntityFrameworkCore;

namespace Business;

public sealed class BusinessModule : IModule, IEfModule
{
    public IReadOnlyCollection<Assembly> ConfigurationAssemblies =>
    [
        typeof(BusinessModule).Assembly
    ];

    public void RegisterServices(ModuleContext context)
    {
        context.Services.AddScoped<CreateOrderService>();
    }
}