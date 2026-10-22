using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Authentication.Domain;

/// <summary>
/// Представляет refresh-токен пользователя.
/// </summary>
public sealed class RefreshToken
{
    /// <summary>
    /// Уникальный идентификатор записи.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Пользователь, которому принадлежит токен.
    /// </summary>
    public AuthUser User { get; set; } = null!;

    /// <summary>
    /// Хеш секрета refresh-токена.
    /// Исходный секрет в базе данных не хранится.
    /// </summary>
    public required string TokenHash { get; set; }

    /// <summary>
    /// Дата истечения срока действия в UTC.
    /// </summary>
    public DateTimeOffset ExpiresAtUtc { get; set; }

    /// <summary>
    /// Дата отзыва токена в UTC или null, если токен активен.
    /// </summary>
    public DateTimeOffset? RevokedAtUtc { get; set; }

    /// <summary>
    /// Идентификатор токена, которым заменён этот токен.
    /// </summary>
    public Guid? ReplacedByTokenId { get; set; }

    /// <summary>
    /// Дата создания токена в UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>
/// Настраивает хранение refresh-токенов.
/// </summary>
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("auth_refresh_tokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.TokenHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(token => token.TokenHash)
            .IsUnique()
            .HasDatabaseName("ux_auth_refresh_tokens_token_hash");

        builder.Property(token => token.ExpiresAtUtc)
            .IsRequired();

        builder.Property(token => token.CreatedAtUtc)
            .IsRequired();

        builder.HasOne(token => token.User)
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(token => new
        {
            token.UserId,
            token.ExpiresAtUtc
        });
        
        builder.Property(token => token.RevokedAtUtc)
            .IsConcurrencyToken();
    }
}