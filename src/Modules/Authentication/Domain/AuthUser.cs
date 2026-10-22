using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Authentication.Domain;

/// <summary>
/// Представляет учётную запись пользователя.
/// </summary>
public sealed class AuthUser
{
    /// <summary>Уникальный идентификатор пользователя.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Адрес электронной почты.</summary>
    public required string Email { get; set; }

    /// <summary>Нормализованный адрес электронной почты.</summary>
    public required string NormalizedEmail { get; set; }

    /// <summary>Хеш пароля.</summary>
    public required string PasswordHash { get; set; }

    /// <summary>Роль пользователя.</summary>
    public AuthUserRole Role { get; set; } = AuthUserRole.User;

    /// <summary>Признак заблокированной учётной записи.</summary>
    public bool IsBlocked { get; set; }
}

/// <summary>
/// Роль пользователя модуля аутентификации.
/// </summary>
public enum AuthUserRole
{
    /// <summary>Обычный пользователь.</summary>
    User = 0,

    /// <summary>Администратор.</summary>
    Administrator = 1
}

/// <summary>
/// Настраивает хранение учётных записей пользователей.
/// </summary>
public sealed class AuthUserConfiguration : IEntityTypeConfiguration<AuthUser>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AuthUser> builder)
    {
        builder.ToTable("auth_users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Email)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(user => user.NormalizedEmail)
            .HasMaxLength(320)
            .IsRequired();

        // builder.HasIndex(user => user.NormalizedEmail)
        //     .IsUnique();

        builder.Property(user => user.PasswordHash)
            .IsRequired();

        builder.Property(user => user.Role)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(user => user.IsBlocked)
            .IsRequired();
        
        builder.HasIndex(user => user.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("ux_auth_users_normalized_email");
    }
}