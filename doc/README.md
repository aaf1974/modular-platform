# Документация Modular Platform

Документация описывает **текущее поведение, подтверждённое исходным кодом**. Архитектурные идеи, которые ещё не реализованы, помечаются отдельно и не должны восприниматься как доступный API.

## С чего начать

- [Обзор архитектуры](architecture/overview.md) — состав платформы и её границы.
- [Модули и их жизненный цикл](architecture/modules.md) — регистрация, зависимости и порядок подключения.
- [Веб-интеграция](architecture/web-integration.md) — `IWebModule`, `UseModules()` и `IPlatformWebFeature`.
- [EF Core](architecture/entity-framework-core.md) — общая модель, аудит, soft delete и репозитории.
- [WebFeature: Health Checks и Swagger](features/web-features.md) — подключение встроенных инфраструктурных возможностей.
- [Демонстрационное приложение](guides/demo-application.md) — `Demo.Api`, `Demo.Business` и миграции.
- [Интеграционные тесты](testing/integration-tests.md) — Testcontainers и `WebApplicationFactory`.
- [Архитектурные правила](standards/architecture-rules.md) — краткие обязательные ограничения.
- [Обзор состояния и замечания](architecture/current-state-review.md) — обнаруженные при ревью особенности и вопросы.
- [Предложения по развитию архитектуры](architecture/architecture-evolution-proposals.md) — системные/бизнес-модули, `BuildPlatform`, WebFeature и порядок middleware (предложения, не текущий API).

## Принципы чтения

1. Исходный код является источником истины для текущего поведения.
2. Архитектурное правило не означает, что оно автоматически обеспечивается кодом или тестом.
3. Примеры команд и кода должны сверяться с актуальной структурой solution.
4. При изменении публичного API, жизненного цикла модулей, EF Core или тестовой инфраструктуры обновляется соответствующий документ.

Старый документ [`Module-Architecture-Rules.md`](Module-Architecture-Rules.md) сохранён как переходная точка к новой структуре.
