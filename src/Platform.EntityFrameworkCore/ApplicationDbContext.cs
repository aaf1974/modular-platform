using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Platform.Core.Modules;
using Platform.EntityFrameworkCore.Auditing;
using Platform.EntityFrameworkCore.Entities;

namespace Platform.EntityFrameworkCore;

/// <summary>Общий контекст базы данных приложения для сущностей всех подключённых модулей.</summary>
/// <remarks>
/// Модули явно регистрируются приложением через механизм Platform.Core.
/// Модули, реализующие <see cref="IEfModule"/>, предоставляют сборки с конфигурациями сущностей.
/// Приложение выбирает провайдер базы данных и строку подключения.
/// </remarks>
public class ApplicationDbContext : DbContext
{
    private readonly IReadOnlyCollection<IModule> _modules;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    
    /// <summary>Ключ набора модулей, влияющего на построение EF-модели.</summary>
    internal string ModelCacheKey { get; }

    /// <summary>Создаёт общий контекст приложения.</summary>
    /// <param name="options">Настройки контекста.</param>
    /// <param name="modules">Экземпляры явно зарегистрированных модулей.</param>
    /// <param name="currentUser">Контракт текущего пользователя.</param>
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IEnumerable<IModule> modules,
        ICurrentUser currentUser, 
        TimeProvider timeProvider)
        : base(options)
    {
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(currentUser);
        _modules = modules.ToArray();
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        ModelCacheKey = CreateModelCacheKey(_modules);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyModuleConfigurations(_modules);
        ApplySoftDeleteFilters(modelBuilder);
    }

    /// <inheritdoc />
    public override int SaveChanges() => SaveChanges(acceptAllChangesOnSuccess: true);

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditInformation();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAuditInformation()
    {
        var now = _timeProvider.GetUtcNow();
        var userId = _currentUser.UserId;

        foreach (var entry in ChangeTracker.Entries().Where(entry =>
                     entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            if (entry.Entity is ISoftDeletable softDeletable && entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                softDeletable.IsDeleted = true;
                softDeletable.DeletedAtUtc = now;
                softDeletable.DeletedByUserId = userId;
            }

            if (entry.State == EntityState.Added)
            {
                if (entry.Entity is ICreatedAt createdAt) createdAt.CreatedAtUtc = now;
                if (entry.Entity is ICreatedBy createdBy) createdBy.CreatedByUserId = userId;
                if (entry.Entity is IModifiedAt modifiedAt) modifiedAt.ModifiedAtUtc = null;
                if (entry.Entity is IModifiedBy modifiedBy) modifiedBy.ModifiedByUserId = null;
            }
            else if (entry.State == EntityState.Modified)
            {
                // Эти значения задаются платформой и не должны меняться при обычном обновлении.
                if (entry.Entity is ICreatedAt && entry.Properties.FirstOrDefault(p => p.Metadata.Name == nameof(ICreatedAt.CreatedAtUtc)) is { } createdAtProperty)
                {
                    createdAtProperty.CurrentValue = createdAtProperty.OriginalValue;
                    createdAtProperty.IsModified = false;
                }
                if (entry.Entity is ICreatedBy && entry.Properties.FirstOrDefault(p => p.Metadata.Name == nameof(ICreatedBy.CreatedByUserId)) is { } createdByProperty)
                {
                    createdByProperty.CurrentValue = createdByProperty.OriginalValue;
                    createdByProperty.IsModified = false;
                }
                if (entry.Entity is IModifiedAt modifiedAt) modifiedAt.ModifiedAtUtc = now;
                if (entry.Entity is IModifiedBy modifiedBy) modifiedBy.ModifiedByUserId = userId;
            }
        }
    }

    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(entityType => entityType.BaseType is null
                         && !entityType.IsOwned()
                         && typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType)))
        {
            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            var isDeleted = Expression.Call(
                typeof(EF),
                nameof(EF.Property),
                [typeof(bool)],
                parameter,
                Expression.Constant(nameof(ISoftDeletable.IsDeleted)));
            Expression filterBody = Expression.Not(isDeleted);

            /*if (entityType.GetQueryFilter() is { } existingFilter)
            {
                var replacedBody = new ReplaceParameterVisitor(existingFilter.Parameters[0], parameter)
                    .Visit(existingFilter.Body)!;
                filterBody = Expression.AndAlso(replacedBody, filterBody);
            }*/
            foreach (var existingFilter in entityType.GetDeclaredQueryFilters().Where(filter => filter.IsAnonymous))
            {
                var existingExpression = existingFilter.Expression;
                var replacedBody = new ReplaceParameterVisitor(existingExpression!.Parameters[0], parameter)
                    .Visit(existingExpression.Body)!;

                filterBody = Expression.AndAlso(replacedBody, filterBody);
            }

            entityType.SetQueryFilter(Expression.Lambda(filterBody, parameter));
        }
    }

    private static string CreateModelCacheKey(IEnumerable<IModule> modules)
    {
        var moduleKeys = new List<string>();
        foreach (var module in modules)
        {
            var moduleKey = module.GetType().AssemblyQualifiedName
                ?? module.GetType().FullName
                ?? module.GetType().Name;
            if (module is IEfModule efModule)
            {
                var assemblies = efModule.ConfigurationAssemblies
                    ?? throw new InvalidOperationException(
                        $"Модуль '{module.GetType().FullName}' вернул null вместо списка EF Core-сборок.");
                moduleKey += "[" + string.Join(";", assemblies.Select(assembly =>
                    assembly?.FullName ?? throw new InvalidOperationException(
                        $"Модуль '{module.GetType().FullName}' содержит null в списке EF Core-сборок."))) + "]";
            }
            moduleKeys.Add(moduleKey);
        }
        return string.Join("|", moduleKeys);
    }

    private sealed class ReplaceParameterVisitor(ParameterExpression source, ParameterExpression target)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == source ? target : base.VisitParameter(node);
    }
}
