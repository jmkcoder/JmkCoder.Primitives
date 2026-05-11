using Microsoft.Extensions.Logging;
using Primitives.Authentication.Strategies.TokenIssuance;

namespace Primitives.Authentication.Client.MessageQueue;

/// <summary>
/// Default <see cref="IMessageTokenAttacher"/> implementation backed by
/// <see cref="ITokenIssuanceService"/>.
/// </summary>
public sealed class MessageTokenAttacher : IMessageTokenAttacher
{
    private readonly ITokenIssuanceService         _tokenService;
    private readonly ILogger<MessageTokenAttacher> _logger;

    public MessageTokenAttacher(
        ITokenIssuanceService         tokenService,
        ILogger<MessageTokenAttacher> logger)
    {
        _tokenService = tokenService;
        _logger       = logger;
    }

    /// <inheritdoc/>
    public async Task<bool> AttachAsync(
        IDictionary<string, string> headers,
        string                      strategyName,
        CancellationToken           cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentException.ThrowIfNullOrWhiteSpace(strategyName);

        var result = await _tokenService.AuthenticateAsync(strategyName, cancellationToken)
                                        .ConfigureAwait(false);

        if (!result.IsSuccess || result.AccessToken is not { Length: > 0 } token)
        {
            _logger.LogWarning(
                "MessageTokenAttacher: failed to acquire token for strategy '{Strategy}': {Error}",
                strategyName, result.ErrorMessage);
            return false;
        }

        headers["Authorization"] = $"Bearer {token}";
        return true;
    }
}
