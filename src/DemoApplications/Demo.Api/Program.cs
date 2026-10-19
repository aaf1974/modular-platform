using Business;
using Microsoft.EntityFrameworkCore;
using Platform.AspNetCore.Configuration;
using Platform.AspNetCore.HealthChecks;
using Platform.AspNetCore.Modules;
using Platform.AspNetCore.OpenApi;
using Platform.Core.Modules;
using Platform.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddModule<BusinessModule>(
    builder.Configuration,
    builder.Environment.EnvironmentName);

builder.Services.AddPlatformEntityFrameworkCore(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.MigrationsAssembly(typeof(Demo.Api.PostgresMigration.MigrationMarker).Assembly)
    );
});

builder.Services.AddPlatform(options =>
    options.AddWebFeature(new SwaggerFeature())
        .AddWebFeature(new HealthCheckFeature(hc => hc.AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!)))
        .AddWebFeature(new HealthCheckUiFeature())
    );

var app = builder.Build();
app.UseModules();

app.MapGet("/", () => Results.Ok(new
{
    Name = "Demo.Api",
    Status = "Running"
}));

app.Run();


public partial class Program;