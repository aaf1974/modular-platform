using System.Security.Claims;
using Authentication.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Authentication.Api;

/// <summary>
/// Предоставляет endpoints регистрации и аутентификации пользователей.
/// </summary>
[ApiController]
[Route("auth")]
[Produces("application/json")]
public sealed class AuthenticationController(
    IAuthenticationService authenticationService,
    IRefreshTokenService refreshTokenService) : ControllerBase
{
    /// <summary>
    /// Регистрирует нового пользователя.
    /// </summary>
    /// <param name="request">Данные регистрации.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Результат регистрации.</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [EnableRateLimiting("auth-register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var created = await authenticationService.RegisterAsync(request.Email, request.Password, cancellationToken);

        if (!created)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Регистрация невозможна",
                Detail = "Проверьте данные или используйте другой адрес электронной почты."
            });
        }

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    /// Выполняет вход и возвращает JWT-токен.
    /// </summary>
    /// <param name="request">Учётные данные пользователя.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Токен доступа и время его истечения.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<RefreshTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [EnableRateLimiting("auth-login")]
    public async Task<ActionResult<RefreshTokenResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.LoginAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (result is null)
        {
            return Unauthorized();
        }

        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType<RefreshTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [EnableRateLimiting("auth-refresh")]
    public async Task<ActionResult<RefreshTokenResponse>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await refreshTokenService.RotateAsync(
            request.RefreshToken,
            cancellationToken);

        if (result is null)
        {
            return Unauthorized();
        }

        return Ok(result);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken)
    {
        var revoked = await refreshTokenService.RevokeAsync(
            request.RefreshToken,
            cancellationToken);

        if (!revoked)
        {
            return Unauthorized();
        }

        return NoContent();
    }
    
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(idValue, out var userId))
        {
            return Unauthorized();
        }

        var changed = await authenticationService.ChangePasswordAsync(
            userId,
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);

        if (!changed)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Не удалось изменить пароль.",
                Detail = "Проверьте текущий пароль и требования к новому паролю.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        return NoContent();
    }
}