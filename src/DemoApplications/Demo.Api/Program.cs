using Business;
using Microsoft.EntityFrameworkCore;
using Platform.AspNetCore.Modules;
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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseModules();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.MapGet("/", () => "Hello World!");

app.MapGet("/", () => Results.Ok(new
{
    Name = "Demo.Api",
    Status = "Running"
}));

app.Run();
