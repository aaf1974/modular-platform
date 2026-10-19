using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Platform.EntityFrameworkCore.Tests;

public sealed class ModelBuilderExtensionsTests
{
    [Fact(DisplayName = "Конфигурация сущности из указанной сборки должна применяться к модели")]
    public void ApplyModuleConfigurations_WhenAssemblyContainsConfiguration_AppliesConfiguration()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);

        Assert.True(
            context.Model.FindEntityType(typeof(TestEntity)) is not null);

        var entityType = context.Model.FindEntityType(typeof(TestEntity))!;

        Assert.Equal("test_entities", entityType.GetTableName());
    }

    [Fact(DisplayName = "Конфигурации нескольких сущностей должны применяться к общей модели")]
    public void ApplyModuleConfigurations_WhenAssemblyContainsMultipleConfigurations_AppliesAllConfigurations()
    {
        var options = new DbContextOptionsBuilder<MultipleEntityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new MultipleEntityDbContext(options);

        var firstEntity = context.Model.FindEntityType(typeof(TestEntity));
        var secondEntity = context.Model.FindEntityType(typeof(SecondTestEntity));

        Assert.NotNull(firstEntity);
        Assert.NotNull(secondEntity);

        Assert.Equal("test_entities", firstEntity!.GetTableName());
        Assert.Equal("second_test_entities", secondEntity!.GetTableName());
    }

    [Fact(DisplayName = "Повторная передача одной сборки не должна приводить к ошибке")]
    public void ApplyModuleConfigurations_WhenAssemblyIsRepeated_DoesNotThrow()
    {
        var modelBuilder = new ModelBuilder();

        var result = modelBuilder.ApplyModuleConfigurations(
            typeof(TestEntityConfiguration).Assembly,
            typeof(TestEntityConfiguration).Assembly);

        Assert.Same(modelBuilder, result);
    }

    [Fact(DisplayName = "Пустой список сборок должен приводить к понятной ошибке")]
    public void ApplyModuleConfigurations_WhenAssembliesAreEmpty_ThrowsArgumentException()
    {
        var modelBuilder = new ModelBuilder();

        var exception = Assert.Throws<ArgumentException>(
            () => modelBuilder.ApplyModuleConfigurations());

        Assert.Contains("хотя бы одну сборку", exception.Message);
    }

    [Fact(DisplayName = "Построитель модели не должен быть null")]
    public void ApplyModuleConfigurations_WhenModelBuilderIsNull_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => ModelBuilderExtensions.ApplyModuleConfigurations(
                null!,
                typeof(TestEntityConfiguration).Assembly));

        Assert.Equal("modelBuilder", exception.ParamName);
    }

    [Fact(DisplayName = "Список сборок не должен быть null")]
    public void ApplyModuleConfigurations_WhenAssembliesAreNull_ThrowsArgumentNullException()
    {
        var modelBuilder = new ModelBuilder();

        var exception = Assert.Throws<ArgumentNullException>(
            //() => modelBuilder.ApplyModuleConfigurations(null!, null!)
            () => modelBuilder.ApplyModuleConfigurations((System.Reflection.Assembly[])null!)
            );

        Assert.Equal("assemblies", exception.ParamName);
    }

    [Fact(DisplayName = "Список сборок не должен содержать null")]
    public void ApplyModuleConfigurations_WhenAssemblyIsNull_ThrowsArgumentException()
    {
        var modelBuilder = new ModelBuilder();

        var exception = Assert.Throws<ArgumentException>(
            () => modelBuilder.ApplyModuleConfigurations(
                typeof(TestEntityConfiguration).Assembly,
                null!));

        Assert.Contains("не должен содержать null", exception.Message);
    }

    [Fact(DisplayName = "Метод должен возвращать исходный построитель модели")]
    public void ApplyModuleConfigurations_ReturnsSameModelBuilder()
    {
        var modelBuilder = new ModelBuilder();

        var result = modelBuilder.ApplyModuleConfigurations(
            typeof(TestEntityConfiguration).Assembly);

        Assert.Same(modelBuilder, result);
    }

    public sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyModuleConfigurations(
                typeof(TestEntityConfiguration).Assembly);
        }
    }

    public sealed class MultipleEntityDbContext
        : DbContext
    {
        public MultipleEntityDbContext(
            DbContextOptions<MultipleEntityDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyModuleConfigurations(
                typeof(TestEntityConfiguration).Assembly);
        }
    }

    public sealed class TestEntity
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public sealed class SecondTestEntity
    {
        public int Id { get; set; }
    }

    public sealed class TestEntityConfiguration
        : IEntityTypeConfiguration<TestEntity>
    {
        public void Configure(EntityTypeBuilder<TestEntity> builder)
        {
            builder.ToTable("test_entities");

            builder.HasKey(entity => entity.Id);

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(200);
        }
    }

    public sealed class SecondTestEntityConfiguration
        : IEntityTypeConfiguration<SecondTestEntity>
    {
        public void Configure(
            EntityTypeBuilder<SecondTestEntity> builder)
        {
            builder.ToTable("second_test_entities");

            builder.HasKey(entity => entity.Id);
        }
    }
}