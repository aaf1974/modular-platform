using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Core.Modules;
using Platform.EntityFrameworkCore;

namespace Platform.EntityFrameworkCore.TestFixtures;

/// <summary>Изолированный тестовый модуль для проверки сборки EF-конфигураций.</summary>
public sealed class SampleEfModule : IModule, IEfModule
{
    /// <inheritdoc />
    public IReadOnlyCollection<Assembly> ConfigurationAssemblies =>
        [typeof(ModuleEntityConfiguration).Assembly];

    /// <inheritdoc />
    public void RegisterServices(ModuleContext context)
    {
    }
}

/// <summary>Сущность, используемая интеграционными и регистрационными тестами.</summary>
public sealed class ModuleEntity : Platform.EntityFrameworkCore.Entities.IEntityId<int>
{
    /// <summary>Первичный ключ.</summary>
    public int Id { get; set; }

    /// <summary>Имя сущности.</summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>Конфигурация тестовой сущности.</summary>
public sealed class ModuleEntityConfiguration : IEntityTypeConfiguration<ModuleEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ModuleEntity> builder)
    {
        builder.ToTable("module_entities");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
    }
}

/// <summary>Тестовая сущность для проверки аудита и мягкого удаления.</summary>
public sealed class AuditedModuleEntity :
    Platform.EntityFrameworkCore.Entities.ICreatedAt,
    Platform.EntityFrameworkCore.Entities.IModifiedAt,
    Platform.EntityFrameworkCore.Entities.ICreatedBy,
    Platform.EntityFrameworkCore.Entities.IModifiedBy,
    Platform.EntityFrameworkCore.Entities.ISoftDeletable
{
    /// <summary>Первичный ключ.</summary>
    public int Id { get; set; }

    /// <summary>Имя сущности.</summary>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedAtUtc { get; set; }

    /// <inheritdoc />
    public string? CreatedByUserId { get; set; }

    /// <inheritdoc />
    public string? ModifiedByUserId { get; set; }

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DeletedAtUtc { get; set; }

    /// <inheritdoc />
    public string? DeletedByUserId { get; set; }
}

/// <summary>Конфигурация тестовой аудируемой сущности.</summary>
public sealed class AuditedModuleEntityConfiguration : IEntityTypeConfiguration<AuditedModuleEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AuditedModuleEntity> builder)
    {
        builder.ToTable("audited_module_entities");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
    }
}
