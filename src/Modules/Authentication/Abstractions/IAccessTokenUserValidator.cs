namespace Authentication.Abstractions;

/// <summary>
/// Проверяет, может ли пользователь использовать выданный ему access-токен.
/// </summary>
public interface IAccessTokenUserValidator
{
    /// <summary>
    /// Проверяет, существует ли пользователь и не заблокирован ли он.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя из токена.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>
    /// <see langword="true"/>, если пользователь существует и не заблокирован;
    /// иначе <see langword="false"/>.
    /// </returns>
    Task<bool> IsActiveAsync(Guid userId, CancellationToken cancellationToken = default);
}