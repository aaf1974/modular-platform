# WebFeature: подключаемые инфраструктурные возможности

## Концепция

`IPlatformWebFeature` — отдельный от `IModule` механизм для инфраструктурных возможностей веб-хоста. Feature реализует два этапа:

- `ConfigureServices(IServiceCollection)` — регистрация сервисов до `Build()`;
- `ConfigureApplication(WebApplication)` — конфигурация построенного приложения.

`AddPlatform(options => ...)` принимает список features. При вызове `UseModules()` они настраиваются до веб-модулей и в порядке регистрации.

## Встроенные возможности

### Swagger / OpenAPI

Проект: `Platform.AspNetCore.OpenApi`.

`SwaggerFeature` регистрирует `AddEndpointsApiExplorer()` и `AddSwaggerGen()`. При настройке приложения включает `UseSwagger()` и `UseSwaggerUI()` только в окружении Development.

### Health Checks

Проект: `Platform.AspNetCore.HealthChecks`.

`HealthCheckFeature` регистрирует health checks и позволяет передать callback `Action<IHealthChecksBuilder>` для подключения проверок зависимостей. Публикует `/health/live` (без запуска проверок зависимостей) и `/health/ready` (все зарегистрированные проверки) с `UIResponseWriter`.

В Demo подключается Npgsql-проверка через `hc.AddNpgSql(connectionString)`.

### Health Checks UI

`HealthCheckUiFeature` регистрирует HealthChecks UI с in-memory storage и endpoint для опроса `"/health/ready"`. UI публикуется по пути `/healthchecks-ui`.

## Подключение в Demo

```csharp
builder.Services.AddPlatform(options => options
    .AddWebFeature(new SwaggerFeature())
    .AddWebFeature(new HealthCheckFeature(
        healthChecks => healthChecks.AddNpgSql(connectionString)))
    .AddWebFeature(new HealthCheckUiFeature()));
```

Далее после `Build()` вызывается `app.UseModules()`.

## Замечания по эксплуатации

- В текущей реализации Swagger включён только в Development.
- `/health/live`, `/health/ready` и `/healthchecks-ui` публикуются без встроенной авторизации. Ограничьте доступ на уровне хоста/инфраструктуры, особенно к UI. Liveness не запускает проверки зависимостей; readiness запускает все проверки, зарегистрированные хостом.
- Health Checks UI использует in-memory storage, поэтому результаты не являются постоянными между перезапусками.
- Хост отвечает за состав зарегистрированных проверок readiness; платформа не навязывает конкретный список зависимостей.
- Регистрируйте каждый экземпляр WebFeature только один раз.
