using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Authentication.Abstractions;
using Authentication.Api;
using Authentication.Configuration;
using Authentication.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Platform.AspNetCore.Modules;
using Platform.Core.Modules;
using Platform.EntityFrameworkCore;
using Platform.EntityFrameworkCore.Auditing;

namespace Authentication;

/// <summary>
/// Представляет модуль аутентификации и управления пользователями.
/// </summary>
public sealed class AuthenticationModule : IModule, IEfModule, IWebModule
{
    public int Order => 100;

    /// <inheritdoc />
    public IReadOnlyCollection<Assembly> ConfigurationAssemblies =>
    [
        typeof(AuthenticationModule).Assembly
    ];

    /// <inheritdoc />
    public void RegisterServices(ModuleContext context)
    {
        context.Services.AddHttpContextAccessor()
            .AddControllers()
            .AddApplicationPart(typeof(AuthenticationController).Assembly);

        ConfigureAuthentication(context);
        ConfigureRateLimited(context);

        context.Services.AddAuthorization()
            .AddScoped<IPasswordHasher<AuthUser>, PasswordHasher<AuthUser>>()
            .AddScoped<IAuthenticationService, AuthenticationService>()
            .AddScoped<IRefreshTokenService, RefreshTokenService>()
            .AddScoped<IJwtTokenService, JwtTokenService>()
            .AddScoped<IAccessTokenUserValidator, AccessTokenUserValidator>()
            .AddScoped<IPasswordResetService, PasswordResetService>()
            .AddScoped<IEmailSender, TestEmailSender>()
            
            .AddScoped<ICurrentUser, HttpCurrentUser>();
    }

    /// <inheritdoc />
    public void ConfigureApplication(WebModuleContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Application.UseRouting();
        context.Application.UseRateLimiter();
        context.Application.UseAuthentication();
        context.Application.UseAuthorization();
        context.Application.MapControllers();
    }
    
    #region Auth

    private static void ConfigureAuthentication(ModuleContext context)
    {
        context.Services
            .AddOptions<AuthenticationOptions>()
            .BindConfiguration(AuthenticationOptions.SectionName)
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(options.Issuer) &&
                    !string.IsNullOrWhiteSpace(options.Audience) &&
                    !string.IsNullOrWhiteSpace(options.SigningKey) &&
                    Encoding.UTF8.GetByteCount(options.SigningKey) >= 32 &&
                    options.TokenLifetimeMinutes is > 0 and <= 1440 &&
                    options.RefreshTokenLifetimeDays is >= 1 and <= 365,
                "Настройки Authentication некорректны.")
            .ValidateOnStart();

        context.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        context.Services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<AuthenticationOptions>>((jwtOptions, configuredOptions) =>
            {
                var auth = configuredOptions.Value;

                jwtOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = auth.Issuer,
                    ValidateAudience = true,
                    ValidAudience = auth.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(auth.SigningKey)),
                    ValidateLifetime = true,
                    // Допустимое расхождение часов между сервером и клиентом.
                    ClockSkew = TimeSpan.FromSeconds(30),
                    // Проверяем, что токен подписан ожидаемым алгоритмом.
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    // Требуем идентификатор пользователя в токене.
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };

                jwtOptions.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async jwtContext =>
                    {
                        var userIdValue = jwtContext.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                                          ?? jwtContext.Principal?.FindFirstValue("sub");

                        if (!Guid.TryParse(userIdValue, out var userId))
                        {
                            jwtContext.Fail("Invalid user identifier.");
                            return;
                        }

                        var validator = jwtContext.HttpContext
                            .RequestServices
                            .GetRequiredService<IAccessTokenUserValidator>();

                        var isActive = await validator.IsActiveAsync(
                            userId,
                            jwtContext.HttpContext.RequestAborted);

                        if (!isActive)
                        {
                            jwtContext.Fail("User is blocked or does not exist.");
                        }
                    }
                };
            });
    }

    #endregion

    #region Rate Limit

    /// <summary>
    /// Регистрирует и настраивает ограничение частоты запросов для аутентификационных endpoint'ов.
    /// </summary>
    private void ConfigureRateLimited(ModuleContext context)
    {
        context.Services.AddOptions<AuthenticationRateLimitOptions>()
            .BindConfiguration(AuthenticationRateLimitOptions.SectionName)
            //.Bind(context.Configuration.GetSection(AuthenticationRateLimitOptions.SectionName))
            .Validate(options =>
                    options is
                    {
                        RegisterPermitLimit: > 0, LoginPermitLimit: > 0, RefreshPermitLimit: > 0, WindowSeconds: > 0
                    },
                "Authentication rate limits must be positive.")
            .ValidateOnStart();

        context.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });
        context.Services
            .AddOptions<RateLimiterOptions>()
            .Configure<IOptions<AuthenticationRateLimitOptions>>((options, configured) =>
            {
                var limits = configured.Value;

                AddFixedWindowPolicy(
                    options,
                    "auth-register",
                    GetClientIp,
                    limits.RegisterPermitLimit,
                    limits.WindowSeconds);

                AddFixedWindowPolicy(
                    options,
                    "auth-login",
                    GetClientIp,
                    limits.LoginPermitLimit,
                    limits.WindowSeconds);

                AddFixedWindowPolicy(
                    options,
                    "auth-refresh",
                    GetClientIp,
                    limits.RefreshPermitLimit,
                    limits.WindowSeconds);

                // options.OnRejected = (context, _) =>
                // {
                //     Console.WriteLine($"Rate limit rejected: {context.HttpContext.Request.Path}");
                //     context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                //     return ValueTask.CompletedTask;
                // };
            });
    }

    /// <summary>
    /// Регистрирует политику ограничения запросов с фиксированным окном.
    /// </summary>
    /// <param name="options">Экземпляр настроек rate limiter.</param>
    /// <param name="policyName">Имя политики ограничения запросов.</param>
    /// <param name="partitionKeyFactory">Функция, которая возвращает ключ сегментации для текущего <see cref="HttpContext"/>.</param>
    /// <param name="permitLimit">Максимальное число разрешённых запросов за одно окно.</param>
    /// <param name="windowSeconds">Длительность окна в секундах.</param>
    static void AddFixedWindowPolicy(RateLimiterOptions options,
        string policyName,
        Func<HttpContext, string> partitionKeyFactory,
        int permitLimit,
        int windowSeconds)
    {
        options.AddPolicy(policyName, httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(partitionKeyFactory(httpContext), _ =>
                new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromSeconds(windowSeconds),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
    }
    
    private static string GetClientIp(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    #endregion
}