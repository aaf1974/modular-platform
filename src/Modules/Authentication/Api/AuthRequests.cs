using System.ComponentModel.DataAnnotations;

namespace Authentication.Api;

/// <summary>Данные для регистрации пользователя.</summary>
/// <param name="Email">Адрес электронной почты.</param>
/// <param name="Password">Пароль.</param>
public sealed record RegisterRequest(
    [param: Required]
    [param: EmailAddress]
    [param: MaxLength(320)]
    string Email,

    [param: Required]
    [param: MinLength(12)]
    [param: MaxLength(128)]
    string Password);

/// <summary>Данные для входа.</summary>
/// <param name="Email">Адрес электронной почты.</param>
/// <param name="Password">Пароль.</param>
public sealed record LoginRequest(
    [param: Required]
    [param: EmailAddress]
    [param: MaxLength(320)]
    string Email,

    [param: Required]
    [param: MaxLength(128)]
    string Password);

/// <summary>Результат успешной аутентификации.</summary>
/// <param name="AccessToken">JWT токен доступа.</param>
/// <param name="ExpiresAtUtc">Время истечения токена в UTC.</param>
public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAtUtc);

/// <summary>Информация о текущем пользователе.</summary>
/// <param name="Id">Идентификатор пользователя.</param>
/// <param name="Email">Адрес электронной почты.</param>
/// <param name="Role">Роль пользователя.</param>
public sealed record CurrentUserResponse(Guid Id, string Email, string Role);

/// <summary>
/// Содержит пару токенов для аутентификации.
/// </summary>
/// <param name="AccessToken">Краткоживущий JWT.</param>
/// <param name="AccessTokenExpiresAtUtc">Время истечения JWT.</param>
/// <param name="RefreshToken">Секрет для обновления пары токенов.</param>
/// <param name="RefreshTokenExpiresAtUtc">Время истечения refresh-токена.</param>
public sealed record RefreshTokenResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);
    
/// <summary>Запрос на обновление пары токенов.</summary>
public sealed record RefreshTokenRequest(
    [Required] string RefreshToken);
    
/// <summary>Запрос на завершение сессии.</summary>
public sealed record LogoutRequest(
    [Required] string RefreshToken);
    
/// <summary>Запрос на смену пароля.</summary>
public sealed record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required]
    [MinLength(12)]
    [MaxLength(128)]
    string NewPassword);

/// <summary>
/// Запрос на восстановление пароля.
/// </summary>
public sealed record ForgotPasswordRequest(
    [param: Required]
    [param: EmailAddress]
    [param: StringLength(254)]
    string Email);

/// <summary>
/// Запрос на установку нового пароля.
/// </summary>
public sealed record ResetPasswordRequest(
    [param: Required]
    [param: StringLength(128, MinimumLength = 12)]
    string NewPassword,
    [param: Required]
    string Token);    