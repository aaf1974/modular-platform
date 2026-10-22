using Authentication.Abstractions;
using Authentication.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.Api;

/// <summary>Управляет учётными записями пользователей.</summary>
[ApiController]
[Route("auth/users")]
[Authorize(Roles = nameof(AuthUserRole.Administrator))]
[Produces("application/json")]
public sealed class AuthenticationUsersController(IAuthenticationService authenticationService) : ControllerBase
{
    /// <summary>Блокирует пользователя.</summary>
    [HttpPost("{userId:guid}/block")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Block(Guid userId, CancellationToken cancellationToken)
    {
        var updated = await authenticationService.SetBlockedAsync(
            userId, true, cancellationToken);

        return updated ? NoContent() : NotFound();
    }

    /// <summary>Разблокирует пользователя.</summary>
    [HttpPost("{userId:guid}/unblock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unblock(Guid userId, CancellationToken cancellationToken)
    {
        var updated = await authenticationService.SetBlockedAsync(
            userId, false, cancellationToken);

        return updated ? NoContent() : NotFound();
    }
}