# Обзор архитектуры

## Цель

Modular Platform — .NET-платформа, в которой приложение явно собирает нужные функциональные модули и инфраструктурные интеграции. Базовые механизмы модулей отделены от ASP.NET Core, EF Core и реализации доставки событий.

## Проекты

| Проект | Ответственность |
|---|---|
| `Platform.Core` | Контракт `IModule`, контекст регистрации, явная регистрация модулей и проверка обязательных зависимостей |
| `Platform.AspNetCore` | Интеграция модулей с `WebApplication`, HTTP-конфигурация и обработка исключений |
| `Platform.AspNetCore.HealthChecks` | WebFeature для endpoint проверки состояния и UI |
| `Platform.AspNetCore.OpenApi` | WebFeature для Swagger/OpenAPI |
| `Platform.EntityFrameworkCore` | Общий `ApplicationDbContext`, сборка модели из конфигураций модулей, аудит, soft delete, репозитории и CRUD-сервис |
| `Platform.Events.Abstractions` | Проект для абстракций событий |
| `Platform.Events.InProcess` | Проект для внутрипроцессной доставки событий |
| `Demo.Api` | Пример host-приложения, которое собирает платформу |
| `Demo.Business` | Демонстрационный бизнес-модуль заказов |
| `Demo.Api.PostgresMigration` | Отдельная сборка миграций для модели Demo-приложения |
| `Platform.Core.Tests` | Тесты ядра и веб-интеграции |
| `Platform.EntityFrameworkCore.Tests` | Тесты EF Core |
| `Platform.EntityFrameworkCore.TestFixtures` | Тестовые модули и EF-конфигурации |
| `Demo.Api.IntegrationTests` | HTTP- и PostgreSQL-интеграционные тесты |

`Authentication`, `Authentication.Contracts` и `Authorization` присутствуют в solution, но в проверенной версии содержат минимальные заготовки (`Class1.cs`), а не полноценные реализованные модули.

## Границы и зависимости

- `Platform.Core` не должен зависеть от ASP.NET Core или EF Core.
- Веб-интеграция находится в `Platform.AspNetCore`.
- EF Core-интеграция находится в `Platform.EntityFrameworkCore`.
- Приложение является composition root: оно выбирает состав модулей, провайдер БД, строку подключения и подключаемые WebFeature.
- Модуль регистрирует свои сервисы через переданный `ModuleContext`.
- Состав модулей задаётся явно. Автоматического поиска модулей по сборкам в текущем механизме регистрации нет.
- `Platform.Events.Abstractions` и `Platform.Events.InProcess` существуют как отдельные проекты, однако в проверенной версии их исходники представлены заглушками. Не следует описывать полноценную событийную шину как реализованную возможность.

## Технологические решения, видимые в коде

- .NET 10 (указан в `Directory.Build.props`).
- Стандартный `Microsoft.Extensions.DependencyInjection`.
- ASP.NET Core `WebApplication`.
- EF Core с выбором провайдера приложением; Demo использует PostgreSQL/Npgsql.
- xUnit и Testcontainers для тестирования; интеграционные тесты Demo используют `WebApplicationFactory<Program>` и PostgreSQL-контейнер.

См. также [текущие замечания](current-state-review.md).
