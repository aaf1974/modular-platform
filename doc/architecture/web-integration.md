# Интеграция модулей с ASP.NET Core

## `IWebModule`

Модуль, которому нужно настраивать HTTP-приложение, дополнительно реализует `Platform.AspNetCore.Modules.IWebModule`:

```csharp
public interface IWebModule
{
    void ConfigureApplication(WebModuleContext context);
}
```

`WebModuleContext` предоставляет построенное приложение (`Application`), сервис-провайдер приложения (`Services`), конфигурацию (`Configuration`) и окружение (`Environment`).

`IWebModule` необязателен: бизнес-модуль может реализовывать только `IModule`.

## Два этапа

1. **До `Build()`**: приложение вызывает `AddModule<TModule>(...)`; `RegisterServices()` добавляет DI-сервисы и, при необходимости, MVC-контроллеры.
2. **После `Build()`**: приложение вызывает `app.UseModules()`; веб-модули конфигурируют приложение через `ConfigureApplication()`.

В текущем `Demo.Api` используется метод `builder.Services.AddModule<BusinessModule>(...)` из `Platform.Core`, а затем `app.UseModules()`.

## Что делает `UseModules()`

Текущая реализация:

1. Запрещает повторный вызов для одного и того же экземпляра `WebApplication`, даже если первый вызов завершился исключением.
2. Вызывает `app.UseExceptionHandler()`.
3. Объединяет `IPlatformWebFeature` и зарегистрированные `IWebModule` в общий список.
4. Сортирует список по `Order`, затем по правилу разрешения равных значений: features перед модулями, регистрационный порядок внутри вида.
5. Логирует начало, ход и завершение настройки; ошибки конфигурации логируются и пробрасываются вызывающему коду.

WebFeature настраиваются **до** веб-модулей.

## Порядок middleware

Положение `UseModules()` относительно других вызовов `Use...` в `Program.cs` остаётся ответственностью приложения. Внутри `UseModules()` сначала добавляется exception handler, затем выполняется конфигурация WebFeature, после чего конфигурируются веб-модули. Порядок важен для middleware и должен быть проверен при добавлении аутентификации, авторизации, CORS и других компонентов конвейера.

## Важная особенность `AddPlatform`

`AddPlatform(options => ...)` регистрирует `IPlatformWebFeature`. Каждый экземпляр feature настраивает сервисы через `ConfigureServices(IServiceCollection)` до построения приложения, а затем `ConfigureApplication(WebApplication)` после `Build()`.

Повторная регистрация одного и того же **экземпляра** feature запрещена. Проверка использует ссылочное равенство; она не запрещает автоматически два разных экземпляра одного типа.

## Пример

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddModule<BusinessModule>(
    builder.Configuration,
    builder.Environment.EnvironmentName);

builder.Services.AddPlatform(options => options
    .AddWebFeature(new SwaggerFeature())
    .AddWebFeature(new HealthCheckFeature()));

var app = builder.Build();
app.UseModules();
app.Run();
```

Пример показывает порядок интеграции, а не полный состав конфигурации для production.


## Возможное развитие порядка конвейера

Порядок middleware является контрактом, который требует отдельного внимания. В текущем API `IWebModule` не имеет свойства `Order`: порядок вызова `ConfigureApplication()` соответствует порядку регистрации модулей, а каждый вызов добавляет middleware в той позиции, в которой он исполняется. WebFeature вызываются раньше веб-модулей; обработчик исключений подключается платформой до них.

Оба контракта имеют `Order` (по умолчанию `0`); меньшие значения конфигурируются раньше. При равном `Order` сохраняется порядок регистрации внутри каждого вида компонентов, а `IPlatformWebFeature` идут перед `IWebModule`. `UseModules()` сортирует оба вида в общем списке. Exception handler подключается перед ними независимо от `Order`. При проектировании зависимых middleware задавайте явные значения `Order` и проверяйте порядок интеграционными тестами.
