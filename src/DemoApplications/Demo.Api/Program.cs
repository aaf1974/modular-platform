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

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is not configured.");

builder.Services.AddPlatformEntityFrameworkCore(options =>
{
    options.UseNpgsql(
        connectionString,
        npgsql => npgsql.MigrationsAssembly(typeof(Demo.Api.PostgresMigration.MigrationMarker).Assembly)
    );
});

builder.Services.AddPlatform(options =>
    options.AddWebFeature(new SwaggerFeature())
        .AddWebFeature(new HealthCheckFeature(hc => hc.AddNpgSql(connectionString)))
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