using Authentication.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.Api;

/// <summary>
/// Предоставляет HTTP-операции восстановления пароля.
/// </summary>
[ApiController]
[Route("auth")]
public sealed class PasswordResetController(
    IPasswordResetService passwordResetService) : ControllerBase
{
    /// <summary>
    /// Запрашивает письмо для восстановления пароля.
    /// </summary>
    /// <param name="request">Адрес электронной почты.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Одинаковый ответ независимо от существования учётной записи.</returns>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await passwordResetService.RequestResetAsync(request.Email, cancellationToken);

        return Ok(new
        {
            Message = "Если учётная запись существует, инструкции будут отправлены на электронную почту."
        });
    }

    /// <summary>
    /// Устанавливает новый пароль по одноразовому токену.
    /// </summary>
    /// <param name="request">Токен восстановления и новый пароль.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Результат операции.</returns>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var succeeded = await passwordResetService.ResetAsync(
            request.Token,
            request.NewPassword,
            cancellationToken);

        return succeeded
            ? NoContent()
            : BadRequest(new { Message = "Токен восстановления недействителен или срок его действия истёк." });
    }
}