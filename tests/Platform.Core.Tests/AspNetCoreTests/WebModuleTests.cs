using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Platform.AspNetCore.Exceptions;
using Platform.AspNetCore.Modules;
using Platform.Core.Modules;

namespace Platform.AspNetCore.Tests;

public sealed partial class WebModuleTests
{
    [Fact(DisplayName = "Веб-модуль должен настраиваться на том же экземпляре, на котором выполнялась регистрация")]
    public void UseModules_UsesSameModuleInstance()
    {
        TestWebModule.Reset();

        var builder = WebApplication.CreateBuilder();

        builder.AddModule<TestWebModule>();

        var registeredInstance = TestWebModule.RegisteredInstance;

        using var app = builder.Build();

        app.UseModules();

        Assert.NotNull(registeredInstance);
        Assert.Same(registeredInstance, TestWebModule.ConfiguredInstance);
    }

    [Fact(DisplayName = "Модуль без веб-контракта не должен участвовать в настройке HTTP")]
    public void UseModules_SkipsModulesWithoutWebContract()
    {
        NonWebModule.Reset();

        var builder = WebApplication.CreateBuilder();

        builder.AddModule<NonWebModule>();

        using var app = builder.Build();

        app.UseModules();

        Assert.True(NonWebModule.RegisterServicesCalled);
    }

    [Fact(DisplayName = "Веб-модуль должен предоставлять HTTP endpoint с ожидаемым ответом")]
    public async Task UseModules_MapsEndpointAndReturnsExpectedResponse()
    {
        var builder = WebApplication.CreateBuilder();

        builder.WebHost.UseTestServer();
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<DefaultExceptionHandler>();
        
        builder.AddModule<TestEndpointModule>();
        builder.ValidateModules();

        await using var app = builder.Build();

        app.UseModules();

        await app.StartAsync();

        var client = app.GetTestClient();

        using var response = await client.GetAsync(
            "/api/business/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<BusinessHealthResponse>();

        Assert.NotNull(result);
        Assert.Equal("Demo.Business", result.Module);
        Assert.Equal("Available", result.Status);
    }

    [Fact(DisplayName = "Веб-модули должны настраиваться по Order с учётом порядка регистрации")]
    public void UseModules_ConfiguresWebModulesInRegistrationOrder()
    {
        var executionOrder = new List<string>();

        TestOrderModuleA.ExecutionOrder = executionOrder;
        TestOrderModuleB.ExecutionOrder = executionOrder;

        var builder = WebApplication.CreateBuilder();

        builder.AddModule<TestOrderModuleA>();
        builder.AddModule<TestOrderModuleB>();

        using var app = builder.Build();

        app.UseModules();

        Assert.Equal(
            ["ModuleB", "ModuleA"],
            executionOrder);
    }
    
    [Fact(DisplayName = "Повторный вызов UseModules должен завершаться понятной ошибкой")]
    public void UseModules_WhenCalledTwice_Throws()
    {
        var builder = WebApplication.CreateBuilder();

        builder.AddModule<TestEndpointModule>();

        using var app = builder.Build();

        app.UseModules();

        var exception = Assert.Throws<InvalidOperationException>(
            () => app.UseModules());

        Assert.Contains("UseModules()", exception.Message);
        Assert.Contains("уже вызывался", exception.Message);
    }
    
    [Fact(DisplayName = "Веб-модуль должен добавлять middleware в HTTP-конвейер")]
    public async Task UseModules_ModuleMiddleware_ExecutesForEndpoint()
    {
        var builder = WebApplication.CreateBuilder();

        builder.WebHost.UseTestServer();
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<DefaultExceptionHandler>();
        
        builder.AddModule<TestMiddlewareModule>();

        await using var app = builder.Build();

        app.UseModules();

        await app.StartAsync();

        var client = app.GetTestClient();
        using var response = await client.GetAsync("/middleware-test");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "executed",
            response.Headers.GetValues("X-Module-Middleware").Single());
    }
    
    /// <summary>
    /// Проверяет, что ошибка настройки веб-модуля пробрасывается,
    /// а повторный вызов UseModules запрещён.
    /// </summary>
    [Fact(DisplayName = "UseModules пробрасывает ошибку веб-модуля и запрещает повторный вызов")]
    public void UseModules_WhenWebModuleConfigurationThrows_ShouldPropagateExceptionAndRejectRetry()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        builder.AddModule<ThrowingWebModule>();

        using var app = builder.Build();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => app.UseModules());

        // Assert
        Assert.Equal(
            "Ошибка настройки тестового веб-модуля.",
            exception.Message);

        var retryException = Assert.Throws<InvalidOperationException>(
            () => app.UseModules());

        Assert.Equal(
            "Метод UseModules() уже вызывался для данного веб-приложения.",
            retryException.Message);
    }
    

    
    /// <summary>
    /// Проверяет, что UseModules выполняется без зарегистрированных модулей
    /// и инфраструктурных возможностей.
    /// </summary>
    [Fact(DisplayName = "UseModules успешно выполняется без зарегистрированных модулей и возможностей")]
    public void UseModules_WhenNoModulesOrFeaturesRegistered_ShouldCompleteSuccessfully()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        using var app = builder.Build();

        // Act
        var exception = Record.Exception(() => app.UseModules());

        // Assert
        Assert.Null(exception);
    }
    
    private sealed record BusinessHealthResponse(string Module, string Status);
    
    public sealed class TestWebModule : IModule, IWebModule
    {
        public static TestWebModule? RegisteredInstance { get; private set; }

        public static TestWebModule? ConfiguredInstance { get; private set; }

        public static void Reset()
        {
            RegisteredInstance = null;
            ConfiguredInstance = null;
        }

        public void RegisterServices(ModuleContext context)
        {
            RegisteredInstance = this;
        }

        public void ConfigureApplication(WebModuleContext context)
        {
            ConfiguredInstance = this;
        }
    }

    public sealed class NonWebModule : IModule
    {
        public static bool RegisterServicesCalled { get; private set; }

        public static void Reset()
        {
            RegisterServicesCalled = false;
        }

        public void RegisterServices(ModuleContext context)
        {
            RegisterServicesCalled = true;
        }
    }
    
    public sealed class TestOrderModuleA : IModule, IWebModule
    {
        public static List<string>? ExecutionOrder { get; set; }
        public int Order => 20;

        public void RegisterServices(ModuleContext context)
        {
        }

        public void ConfigureApplication(WebModuleContext context)
        {
            ExecutionOrder!.Add("ModuleA");
        }
    }

    public sealed class TestOrderModuleB : IModule, IWebModule
    {
        public static List<string>? ExecutionOrder { get; set; }
        public int Order => 5;

        public void RegisterServices(ModuleContext context)
        {
        }

        public void ConfigureApplication(WebModuleContext context)
        {
            ExecutionOrder!.Add("ModuleB");
        }
    }
    
    public sealed class TestMiddlewareModule : IModule, IWebModule
    {
        public void RegisterServices(ModuleContext context)
        {
        }

        public void ConfigureApplication(WebModuleContext context)
        {
            context.Application.Use(async (httpContext, next) =>
            {
                httpContext.Response.Headers["X-Module-Middleware"] = "executed";

                await next();
            });

            context.Application.MapGet(
                "/middleware-test",
                () => Results.Ok(new { Status = "Available" }));
        }
    }
    
    /// <summary>
    /// Тестовый веб-модуль, который выбрасывает исключение при настройке приложения.
    /// </summary>
    private sealed class ThrowingWebModule : IModule, IWebModule
    {
        /// <summary>
        /// Выбрасывает исключение при настройке HTTP-конвейера.
        /// </summary>
        /// <param name="context">Контекст настройки веб-модуля.</param>
        public void ConfigureApplication(WebModuleContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            throw new InvalidOperationException("Ошибка настройки тестового веб-модуля.");
        }

        public void RegisterServices(ModuleContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
        }
    }
}

