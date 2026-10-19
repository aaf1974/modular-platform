namespace Platform.EntityFrameworkCore.Entities;

/// <summary>Определяет сущность, для которой поддерживается мягкое удаление.</summary>
public interface ISoftDeletable
{
    /// <summary>Признак мягкого удаления.</summary>
    bool IsDeleted { get; set; }

    /// <summary>Время мягкого удаления в UTC.</summary>
    DateTimeOffset? DeletedAtUtc { get; set; }

    /// <summary>Идентификатор пользователя, выполнившего мягкое удаление.</summary>
    string? DeletedByUserId { get; set; }
}
