namespace Authentication.Configuration;

/// <summary>Настройки JWT-аутентификации.</summary>
public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";
    
    /// <summary>Издатель токенов.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Аудитория токенов.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Секрет подписи токенов.</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Время жизни токена в минутах.</summary>
    public int TokenLifetimeMinutes { get; set; } = 30;
    
    /// <summary>
    /// Срок действия refresh-токена в днях.
    /// </summary>
    public int RefreshTokenLifetimeDays { get; set; } = 14;
}

public sealed class AuthenticationRateLimitOptions
{
    public const string SectionName = "Authentication:RateLimiting";

    public int RegisterPermitLimit { get; set; } = 30;

    public int LoginPermitLimit { get; set; } = 30;

    public int RefreshPermitLimit { get; set; } = 30;

    public int WindowSeconds { get; set; } = 60;
}