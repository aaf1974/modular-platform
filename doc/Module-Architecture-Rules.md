# Документация архитектуры модулей — переходная страница

Ранее этот файл объединял архитектурные принципы, правила разработки, описание ASP.NET Core, EF Core и тестирования. Документация разделена на тематические файлы, чтобы проще было поддерживать соответствие коду.

Начните с [`doc/README.md`](README.md), затем используйте:

- [Обзор архитектуры](architecture/overview.md)
- [Модули и их жизненный цикл](architecture/modules.md)
- [Веб-интеграция](architecture/web-integration.md)
- [EF Core](architecture/entity-framework-core.md)
- [WebFeature](features/web-features.md)
- [Demo-приложение](guides/demo-application.md)
- [Интеграционные тесты](testing/integration-tests.md)
- [Архитектурные правила](standards/architecture-rules.md)
- [Результаты первичного ревью](architecture/current-state-review.md)

При обновлении документации редактируйте тематический файл, а не восстанавливайте прежний монолитный документ.
