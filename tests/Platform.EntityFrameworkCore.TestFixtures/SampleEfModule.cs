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
public sealed class ModuleEntity
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
