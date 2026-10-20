# Demo-приложение

## Состав

- `src/DemoApplications/Demo.Api` — ASP.NET Core host и composition root.
- `src/DemoApplications/Demo.Business` — `BusinessModule`, сущности заказа, сервисы и MVC controller.
- `src/DemoApplications/Demo.Api.PostgresMigration` — EF Core migration assembly.
- `tests/Demo.Api.IntegrationTests` — HTTP-интеграционные тесты с PostgreSQL через Testcontainers.

## Компоновка `Program.cs`

Demo.Api:

1. Создаёт `WebApplicationBuilder`.
2. Регистрирует `BusinessModule` через `builder.Services.AddModule`, передавая конфигурацию и имя окружения.
3. Получает `ConnectionStrings:DefaultConnection`; если значение отсутствует, выбрасывает `InvalidOperationException`.
4. Регистрирует `ApplicationDbContext` через `AddPlatformEntityFrameworkCore` с Npgsql и отдельной сборкой миграций.
5. Регистрирует Swagger, Health Checks и Health Checks UI через `AddPlatform`.
6. Строит приложение и вызывает `app.UseModules()`.
7. Публикует корневой `GET /`, возвращающий имя приложения и статус.

В текущем `Program.cs` не вызывается `ValidateModules()`. Если появятся модули с обязательными зависимостями, рекомендуется вызвать валидацию до `Build()`.

## Бизнес-модуль

`BusinessModule` реализует `IModule`, `IEfModule` и `IWebModule`:

- регистрирует `CreateOrderService` и `GetOrderByIdService` как scoped;
- регистрирует MVC controllers и assembly part;
- предоставляет свою сборку для EF-конфигураций;
- вызывает `MapControllers()` при `ConfigureApplication()`.

Контроллер `OrdersController` публикует `POST /api/orders` и `GET /api/orders/{id}`. Создание заказа создаёт заказ и позиции, добавляет агрегат через репозиторий и вызывает `SaveChangesAsync()` один раз.

## Конфигурация

Строка подключения берётся из `ConnectionStrings:DefaultConnection`. В архиве есть `appsettings.json` и `appsettings.Development.json`; перед запуском проверьте локальную строку подключения и не храните реальные секреты в репозитории.

## Миграции

Миграции находятся в `Demo.Api.PostgresMigration/Migrations`. Пример команды для добавления миграции из корня репозитория:

```bash
dotnet ef migrations add <MigrationName> \
  --project src/DemoApplications/Demo.Api.PostgresMigration/Demo.Api.PostgresMigration.csproj \
  --startup-project src/DemoApplications/Demo.Api/Demo.Api.csproj \
  --output-dir Migrations
```

Применение миграций:

```bash
dotnet ef database update \
  --project src/DemoApplications/Demo.Api.PostgresMigration/Demo.Api.PostgresMigration.csproj \
  --startup-project src/DemoApplications/Demo.Api/Demo.Api.csproj
```

Команды требуют установленного `dotnet-ef`, доступной PostgreSQL и корректной конфигурации приложения. Перед запуском рекомендуется сверить параметры с версиями EF Core в `.csproj`.

## Ограничения примера

Demo предназначено для демонстрации архитектурной композиции и persistence. Не следует трактовать его как production-ready API: обработка бизнес-валидации, политика доступа, безопасность диагностических endpoints и правила эксплуатации требуют отдельного проектирования.
