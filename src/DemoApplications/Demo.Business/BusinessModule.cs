using System.Reflection;
using Business.Controllers;
using Business.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Platform.AspNetCore.Modules;
using Platform.Core.Modules;
using Platform.EntityFrameworkCore;

namespace Business;

public sealed class BusinessModule : IModule, IEfModule, IWebModule
{
    public IReadOnlyCollection<Assembly> ConfigurationAssemblies =>
    [
        typeof(BusinessModule).Assembly
    ];

    public void RegisterServices(ModuleContext context)
    {
        context.Services.AddScoped<CreateOrderService>();
        
        context.Services
            .AddControllers()
            .AddApplicationPart(typeof(OrdersController).Assembly);
    }

    public void ConfigureApplication(WebModuleContext context)
    {
        context.Application.MapControllers();
        // context.Application.MapOrderEndpoints();
        //throw new NotImplementedException();
    }
}