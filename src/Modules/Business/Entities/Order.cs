using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.EntityFrameworkCore.Entities;

namespace Business.Entities;

/// <summary>
/// Заказ, содержащий одну или несколько позиций.
/// </summary>
public sealed class Order : IEntityId<Guid>, 
    ISoftDeletable, ICreatedAt, IModifiedAt, ICreatedBy, IModifiedBy
{
    /// <inheritdoc />
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Номер заказа.
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Дата и время создания заказа в UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Позиции заказа.
    /// </summary>
    public ICollection<OrderDetail> Details { get; set; } =
        new List<OrderDetail>();
    
    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public string? DeletedByUserId { get; set; }

    public DateTimeOffset? ModifiedAtUtc { get; set; }
    public string? CreatedByUserId { get; set; }
    public string? ModifiedByUserId { get; set; }
}

/// <summary>
/// Позиция заказа.
/// </summary>
public sealed class OrderDetail : IEntityId<Guid>, ISoftDeletable, ICreatedAt, IModifiedAt
{
    /// <inheritdoc />
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Идентификатор заказа.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Наименование товара.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Количество единиц товара.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Цена одной единицы товара.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Заказ, которому принадлежит позиция.
    /// </summary>
    public Order Order { get; set; } = null!;
    
    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public string? DeletedByUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ModifiedAtUtc { get; set; }
}

public sealed class PhysicalDeleteEntity : IEntityId<Guid>
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Конфигурация заказа.
/// </summary>
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(order => order.Id);

        builder.Property(order => order.OrderNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(order => order.OrderNumber)
            .IsUnique();

        builder.Property(order => order.CreatedAtUtc)
            .IsRequired();

        builder.HasMany(order => order.Details)
            .WithOne(detail => detail.Order)
            .HasForeignKey(detail => detail.OrderId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Конфигурация позиции заказа.
/// </summary>
public sealed class OrderDetailConfiguration : IEntityTypeConfiguration<OrderDetail>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OrderDetail> builder)
    {
        builder.ToTable(
            "order_details",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_order_details_quantity_positive",
                    "\"Quantity\" > 0");

                table.HasCheckConstraint(
                    "CK_order_details_unit_price_non_negative",
                    "\"UnitPrice\" >= 0");
            });

        builder.HasKey(detail => detail.Id);

        builder.Property(detail => detail.ProductName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(detail => detail.Quantity)
            .IsRequired();

        builder.Property(detail => detail.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(detail => detail.OrderId);
    }
    
    public sealed class PhysicalDeleteEntityConfiguration
        : IEntityTypeConfiguration<PhysicalDeleteEntity>
    {
        public void Configure(
            EntityTypeBuilder<PhysicalDeleteEntity> builder)
        {
            builder.ToTable("physical_delete_entities");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();
        }
    }
}