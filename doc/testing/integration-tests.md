# Тестирование

## Проекты тестов

- `Platform.Core.Tests` — регистрация модулей, конфигурация платформы, веб-модули, обработчик исключений и вспомогательные расширения.
- `Platform.EntityFrameworkCore.Tests` — тесты модели и `ApplicationDbContext`, включая PostgreSQL-сценарии.
- `Demo.Api.IntegrationTests` — проверки HTTP endpoints и интеграции Demo с PostgreSQL. Так же содержит интеграционные тесты на базе TestContainers, демонстрирующие поведение функционала платформы.
- `Platform.EntityFrameworkCore.TestFixtures` — тестовые модули и конфигурации для изоляции тестовой модели.

## Интеграционные тесты Demo

`DemoApiFixture`:

1. Запускает контейнер `postgres:16-alpine` через Testcontainers.
2. Создаёт `WebApplicationFactory<Program>`.
3. Подменяет `ConnectionStrings:DefaultConnection` строкой подключения контейнера через in-memory configuration.
4. Создаёт `HttpClient`.
5. Вызывает `Database.MigrateAsync()` для применения миграций.
6. Освобождает клиент, фабрику приложения и PostgreSQL-контейнер в `DisposeAsync()`.

Fixture подключена к xUnit collection, что позволяет тестам одной коллекции использовать общий экземпляр инфраструктуры. Следовательно, тесты должны учитывать общее состояние БД и изолировать данные между сценариями.

## Что покрывается в текущем архиве

По именам и содержимому файлов присутствуют проверки:

- ответов endpoints заказов;
- Swagger;
- `/health/live`, `/health/ready` и Health Checks UI;
- обработки исключений;
- бизнес-модуля, аудита и сервисов;
- поведения `ApplicationDbContext` и конфигурации модели.

Это описание структуры тестов, а не подтверждение их успешного прохождения в данной среде.

## Запуск

Из корня репозитория:

```bash
dotnet restore ModularPlatform.slnx
dotnet build ModularPlatform.slnx --no-restore
dotnet test ModularPlatform.slnx --no-build
```

Для интеграционных тестов требуется работающий Docker Engine и возможность загрузить образ PostgreSQL, если он ещё не кэширован.

В среде анализа архива `dotnet` не был установлен, поэтому сборку и тесты здесь выполнить не удалось. Перед объединением изменений запускайте эти команды в среде разработки/CI.

## Рекомендации

- Тесты, зависящие от поведения PostgreSQL, выполняйте на PostgreSQL через Testcontainers, а не только на EF InMemory.
- Изолируйте данные при использовании общей fixture.
- Проверяйте поведение на уровне HTTP для middleware, маршрутов и WebFeature.
- Критичные архитектурные ограничения проверяйте автоматическими тестами, а не только документацией.
