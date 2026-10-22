using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Platform.EntityFrameworkCore.Auditing;

namespace Authentication;

/// <summary>
/// Определяет текущего пользователя на основании аутентифицированного HTTP-запроса.
/// </summary>
internal sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public string? UserId
    {
        get
        {
            var principal = _httpContextAccessor.HttpContext?.User;

            if (principal?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return principal.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? principal.FindFirstValue("sub");
        }
    }
}