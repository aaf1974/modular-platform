using System.Security.Claims;
using Authentication.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Platform.EntityFrameworkCore;

namespace Authentication.Api;

/// <summary>
/// Предоставляет сведения о текущем пользователе.
/// </summary>
[ApiController]
[Route("auth/me")]
[Authorize]
[Produces("application/json")]
public sealed class CurrentUserController(ApplicationDbContext dbContext) : ControllerBase
{
    /// <summary>
    /// Возвращает профиль текущего аутентифицированного пользователя.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Профиль пользователя.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponse>> Get(CancellationToken cancellationToken)
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(idValue, out var id))
        {
            return Unauthorized();
        }

        var user = await dbContext.Set<AuthUser>()
            .Where(item => item.Id == id && !item.IsBlocked)
            .Select(item => new CurrentUserResponse(
                item.Id,
                item.Email,
                item.Role.ToString()))
            .SingleOrDefaultAsync(cancellationToken);

        return user is null ? Unauthorized() : Ok(user);
    }
}