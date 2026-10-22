using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Authentication.Abstractions;
using Authentication.Api;
using Authentication.Configuration;
using Authentication.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Authentication;

/// <summary>Создаёт подписанные JWT-токены доступа.</summary>
internal sealed class JwtTokenService(IOptions<AuthenticationOptions> options, TimeProvider timeProvider)
    : IJwtTokenService
{
    public LoginResponse CreateToken(AuthUser user)
    {
        var settings = options.Value;
        var now = timeProvider.GetUtcNow();
        var expires = now.AddMinutes(settings.TokenLifetimeMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(settings.SigningKey));

        var credentials = new SigningCredentials(
            key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        return new LoginResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expires);
    }
}