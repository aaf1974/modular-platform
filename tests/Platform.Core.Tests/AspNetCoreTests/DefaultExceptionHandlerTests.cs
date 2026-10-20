using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.AspNetCore.Exceptions;

namespace Platform.AspNetCore.Tests.Exceptions;

public sealed class DefaultExceptionHandlerTests
{
    [Fact(DisplayName = "Development returns exception details in ProblemDetails")]
    public async Task TryHandleAsync_ShouldIncludeExceptionDetails_WhenDevelopment()
    {
        using var provider = CreateServiceProvider();

        var context = CreateHttpContext(provider);
        var handler = CreateHandler(provider, Environments.Development);

        var exception = CreateException();

        var handled = await handler.TryHandleAsync(
            context,
            exception,
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError,
            context.Response.StatusCode);

        var json = await ReadResponseAsync(context);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            root.GetProperty("status").GetInt32());

        Assert.Equal(
            exception.Message,
            root.GetProperty("detail").GetString());

        Assert.Equal(
            exception.GetType().FullName,
            root.GetProperty("exceptionType").GetString());

        Assert.False(string.IsNullOrWhiteSpace(
            root.GetProperty("stackTrace").GetString()));

        Assert.Equal(
            "test-trace-id",
            root.GetProperty("traceId").GetString());
    }

    [Fact(DisplayName = "Production does not expose exception details in ProblemDetails")]
    public async Task TryHandleAsync_ShouldHideExceptionDetails_WhenProduction()
    {
        using var provider = CreateServiceProvider();

        var context = CreateHttpContext(provider);
        var handler = CreateHandler(provider, Environments.Production);

        var exception = CreateException();

        var handled = await handler.TryHandleAsync(
            context,
            exception,
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError,
            context.Response.StatusCode);

        var json = await ReadResponseAsync(context);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            root.GetProperty("status").GetInt32());

        Assert.False(root.TryGetProperty("detail", out _));
        Assert.False(root.TryGetProperty("exceptionType", out _));
        Assert.False(root.TryGetProperty("stackTrace", out _));

        Assert.Equal(
            "test-trace-id",
            root.GetProperty("traceId").GetString());

        Assert.DoesNotContain(exception.Message, json);
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddProblemDetails();

        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext CreateHttpContext(
        IServiceProvider serviceProvider)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = serviceProvider,
            TraceIdentifier = "test-trace-id"
        };

        context.Request.Headers["Accept"] = "application/json";
        context.Response.Body = new MemoryStream();

        return context;
    }

    private static DefaultExceptionHandler CreateHandler(
        IServiceProvider serviceProvider,
        string environmentName)
    {
        var logger = serviceProvider
            .GetRequiredService<ILogger<DefaultExceptionHandler>>();

        var problemDetailsService = serviceProvider
            .GetRequiredService<IProblemDetailsService>();

        var environment = new TestWebHostEnvironment
        {
            EnvironmentName = environmentName
        };

        return new DefaultExceptionHandler(
            problemDetailsService,
            logger,
            environment);
    }

    private static Exception CreateException()
    {
        try
        {
            throw new InvalidOperationException(
                "Sensitive internal exception details");
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private static async Task<string> ReadResponseAsync(
        HttpContext context)
    {
        context.Response.Body.Position = 0;

        using var reader = new StreamReader(
            context.Response.Body,
            leaveOpen: true);

        return await reader.ReadToEndAsync();
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Platform.Tests";

        public IFileProvider WebRootFileProvider { get; set; } =
            new NullFileProvider();

        public string WebRootPath { get; set; } = string.Empty;

        public string EnvironmentName { get; set; } =
            Environments.Production;

        public string ContentRootPath { get; set; } =
            AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } =
            new NullFileProvider();
    }
}
