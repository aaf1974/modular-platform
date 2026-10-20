# Demo.Api

## Создание миграции

Запускать из корня репозитория:

```bash
dotnet ef migrations add <MigrationName> \
  --project src/DemoApplications/Demo.Api.PostgresMigration/Demo.Api.PostgresMigration.csproj \
  --startup-project src/DemoApplications/Demo.Api/Demo.Api.csproj \
  --output-dir Migrations
```
```bash
dotnet ef migrations add <TestMigration> --context ApplicationDbContext  --project src/DemoApplications/Demo.Api.PostgresMigration/Demo.Api.PostgresMigration.csproj   --startup-project src/DemoApplications/Demo.Api/Demo.Api.csproj   --output-dir Migrations
```

## Применение миграций

```bash
dotnet ef database update \
  --project src/DemoApplications/Demo.Api.PostgresMigration/Demo.Api.PostgresMigration.csproj \
  --startup-project src/DemoApplications/Demo.Api/Demo.Api.csproj
```

Нужны установленный `dotnet-ef`, доступная PostgreSQL и корректная `ConnectionStrings:DefaultConnection`. В `Demo.Api` провайдер PostgreSQL и сборка миграций настраиваются в `Program.cs`.

Для архитектурного обзора и описания endpoints см. [`doc/guides/demo-application.md`](../../../doc/guides/demo-application.md).


## Диагностика

- `GET /health/live` — liveness: подтверждает, что приложение отвечает, не выполняя проверки зависимостей.
- `GET /health/ready` — readiness: выполняет проверки, зарегистрированные хостом (в Demo — PostgreSQL).
- `/healthchecks-ui` — Health Checks UI, опрашивающий `/health/ready`. Встроенная авторизация отсутствует; ограничьте доступ на уровне хоста или инфраструктуры.
