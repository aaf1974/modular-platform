# EF Core: модель приложения и persistence

## Общая модель

`Platform.EntityFrameworkCore` предоставляет `ApplicationDbContext`, общий для сущностей явно подключённых модулей. Модуль, которому нужна конфигурация EF Core, может реализовать `IEfModule` и предоставить `ConfigurationAssemblies`.

Во время `OnModelCreating` контекст применяет `IEntityTypeConfiguration<TEntity>` из этих сборок. Модуль без EF Core не обязан реализовывать `IEfModule`. Автоматического поиска всех загруженных сборок нет.

## Настройка приложения

Приложение выбирает провайдер и строку подключения:

```csharp
builder.Services.AddPlatformEntityFrameworkCore(options =>
{
    options.UseNpgsql(connectionString,
        npgsql => npgsql.MigrationsAssembly(migrationsAssembly));
});
```

`AddPlatformEntityFrameworkCore` регистрирует `TimeProvider.System` и `StubCurrentUser` через `TryAdd`, а также scoped открытые generic-регистрации `IRepository<,>`, `IEfRepository<,>` и `ICrudService<,>`. Метод вызывает `AddDbContextFactory<ApplicationDbContext>` с `ServiceLifetime.Scoped`; контекст также доступен как scoped-сервис.

## Ключ кэша модели

EF Core по умолчанию кэширует модель по типу контекста. Платформа подменяет `IModelCacheKeyFactory`, чтобы ключ `ApplicationDbContext` учитывал типы подключённых модулей и предоставленные ими сборки конфигураций. Поэтому состав модулей, влияющих на модель, должен быть стабилен в рамках одного приложения.

## Аудит

Сущности могут реализовывать отдельные контракты:

- `ICreatedAt`, `ICreatedBy`;
- `IModifiedAt`, `IModifiedBy`.

`ApplicationDbContext` заполняет эти поля при `SaveChanges` и `SaveChangesAsync`, используя `TimeProvider` и `ICurrentUser`. `CreatedAt` и `CreatedBy` при обычном обновлении сохраняются неизменными. Для добавляемой сущности поля модификации устанавливаются в `null`.

`ICurrentUser.UserId` — nullable string. По умолчанию используется `StubCurrentUser`; приложение/модуль может зарегистрировать реальную реализацию. При нескольких регистрациях необходимо осознанно контролировать разрешаемый сервис.

## Soft delete

Сущности, реализующие `ISoftDeletable`, получают обработку удаления: `Remove` преобразуется в обновление с `IsDeleted = true`, `DeletedAtUtc` и `DeletedByUserId`. Для корневых типов сущностей создаётся глобальный query filter, исключающий помеченные записи.

`IgnoreQueryFilters()` отключает все глобальные фильтры для запроса, а не только soft-delete. При построении модели платформа должна сохранять пользовательский фильтр сущности, объединяя его с условием `!IsDeleted`. Для этого поведения добавлены регрессионные тесты: обычный запрос должен исключать как мягко удалённые записи, так и записи, отсекаемые пользовательским фильтром; `IgnoreQueryFilters()` должен отключать оба фильтра. Эти проверки на InMemory-провайдере проверяют модельную логику, но не заменяют PostgreSQL-интеграционные тесты для провайдер-зависимого поведения.

## Репозитории и единица работы

Платформа регистрирует:

- `IRepository<TEntity, TId>`;
- `IEfRepository<TEntity, TId>`;
- `ICrudService<TEntity, TId>`.

`CrudService` делегирует операции репозиторию. Репозитории не должны самостоятельно фиксировать изменения: сохранение выполняет прикладной сценарий через `ApplicationDbContext.SaveChanges/SaveChangesAsync`, чтобы объединить несколько изменений одной единицей работы.

`IRepository` не раскрывает `IQueryable`; EF-специфичный доступ предоставляется через `IEfRepository`. Стандартный список сортируется по идентификатору перед пагинацией, размер страницы ограничен диапазоном 1–500.

Для замены реализации одного типа сущности предпочтительна закрытая регистрация, например `IRepository<Order, Guid>`. `IRepository<,>` и `IEfRepository<,>` — отдельные DI-контракты, и замена одного не заменяет другой.

## Миграции

В Demo миграции вынесены в отдельный проект `Demo.Api.PostgresMigration`, а `Demo.Api` указывает его сборку через `MigrationsAssembly`. Миграции относятся к итоговой модели приложения и не должны автоматически применяться модулем при обычной регистрации сервисов. В интеграционных тестах Demo миграции применяются вызовом `Database.MigrateAsync()`.
