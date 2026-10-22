using Authentication.Api;
using Authentication.Domain;

namespace Authentication.Abstractions;

/// <summary>Создаёт access-токены.</summary>
public interface IJwtTokenService
{
    /// <summary>Создаёт access-токен для пользователя.</summary>
    LoginResponse CreateToken(AuthUser user);
}