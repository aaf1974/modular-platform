using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Authentication.Domain;

/// <summary>
/// Одноразовый токен для восстановления пароля пользователя.
/// </summary>
public sealed class PasswordResetToken
{
    /// <summary>Уникальный идентификатор токена.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Идентификатор пользователя.</summary>
    public Guid UserId { get; set; }

    /// <summary>SHA-256-хеш исходного токена.</summary>
    public required string TokenHash { get; set; }

    /// <summary>Время создания токена в UTC.</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Время окончания действия в UTC.</summary>
    public DateTimeOffset ExpiresAtUtc { get; set; }

    /// <summary>Время использования токена в UTC; null, если токен не использован.</summary>
    public DateTimeOffset? UsedAtUtc { get; set; }

    /// <summary>Пользователь, которому принадлежит токен.</summary>
    public AuthUser User { get; set; } = null!;
}


/// <summary>
/// Конфигурация сущности токена восстановления пароля.
/// </summary>
public sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("auth_password_reset_tokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.TokenHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(token => token.TokenHash)
            .IsUnique()
            .HasDatabaseName("ux_auth_password_reset_tokens_hash");

        builder.Property(token => token.CreatedAtUtc).IsRequired();
        builder.Property(token => token.ExpiresAtUtc).IsRequired();

        builder.Property(token => token.UsedAtUtc)
            .IsConcurrencyToken();

        builder.HasOne(token => token.User)
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(token => new
        {
            token.UserId,
            token.ExpiresAtUtc
        });
    }
}