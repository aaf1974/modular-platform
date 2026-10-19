using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Platform.EntityFrameworkCore;

/// <summary>
/// Создаёт ключ кэша модели с учётом состава модулей приложения.
/// </summary>
/// <remarks>
/// EF Core по умолчанию кэширует модель по типу контекста. В одном процессе
/// могут создаваться несколько контейнеров с разными наборами модулей, поэтому
/// для общего контекста состав модулей также должен участвовать в ключе.
/// </remarks>
public sealed class ApplicationModelCacheKeyFactory : IModelCacheKeyFactory
{
    /// <inheritdoc />
    public object Create(DbContext context, bool designTime)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context is ApplicationDbContext applicationContext
            ? (context.GetType(), applicationContext.ModelCacheKey, designTime)
            : (context.GetType(), designTime);
    }
}
