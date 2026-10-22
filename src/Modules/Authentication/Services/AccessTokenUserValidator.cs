using Authentication.Abstractions;
using Authentication.Domain;
using Microsoft.EntityFrameworkCore;
using Platform.EntityFrameworkCore;

namespace Authentication;

/// <inheritdoc />
internal sealed class AccessTokenUserValidator(ApplicationDbContext dbContext) : IAccessTokenUserValidator
{
    /// <inheritdoc />
    public Task<bool> IsActiveAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return dbContext.Set<AuthUser>()
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId && !user.IsBlocked, cancellationToken);
    }
}