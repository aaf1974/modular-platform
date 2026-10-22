using System.Collections.Concurrent;
using Authentication.Abstractions;

namespace Authentication;

/// <summary>
/// Сохраняет письма в памяти для интеграционных тестов.
/// </summary>
internal sealed class TestEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<(string Email, string Token)> _messages = new();

    /// <summary>Отправленные тестовые письма.</summary>
    public IReadOnlyCollection<(string Email, string Token)> Messages =>
        _messages.ToArray();

    /// <inheritdoc />
    public Task SendPasswordResetAsync(
        string email,
        string resetToken,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _messages.Enqueue((email, resetToken));
        return Task.CompletedTask;
    }
}