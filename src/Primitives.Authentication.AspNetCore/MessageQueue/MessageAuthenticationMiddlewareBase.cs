using Microsoft.Extensions.Logging;
using Primitives.Authentication.Strategies.TokenIssuance;
using System.Security.Claims;

namespace Primitives.Authentication.AspNetCore.MessageQueue;

/// <summary>
/// Base class for transport-specific message-queue authentication middleware.
/// Subclass this, inject into your consumer, and call
/// <see cref="AuthenticateAsync"/> before processing each message.
/// </summary>
/// <typeparam name="TContext">
/// Your transport-specific <see cref="IMessageAuthenticationContext"/> implementation.
/// </typeparam>
/// <example>
/// <code>
/// public sealed class MyRabbitConsumer
///     : MessageAuthenticationMiddlewareBase&lt;RabbitMessageAuthContext&gt;
/// {
///     public MyRabbitConsumer(IJwtTokenValidator v, ILogger&lt;MyRabbitConsumer&gt; l)
///         : base(v, l) { }
///
///     public async Task ConsumeAsync(IBasicProperties props, byte[] body)
///     {
///         var ctx = new RabbitMessageAuthContext(props);
///         if (!await AuthenticateAsync(ctx))
///             return; // rejected
///
///         // Principal is now available
///         var userId = Principal!.FindFirstValue(ClaimTypes.NameIdentifier);
///     }
/// }
/// </code>
/// </example>
public abstract class MessageAuthenticationMiddlewareBase<TContext>
    where TContext : IMessageAuthenticationContext
{
    private readonly IJwtTokenValidator _validator;
    private readonly ILogger            _logger;

    protected MessageAuthenticationMiddlewareBase(
        IJwtTokenValidator validator,
        ILogger            logger)
    {
        _validator = validator;
        _logger    = logger;
    }

    /// <summary>
    /// The <see cref="ClaimsPrincipal"/> populated after a successful
    /// <see cref="AuthenticateAsync"/> call.  <c>null</c> until authentication succeeds.
    /// </summary>
    protected ClaimsPrincipal? Principal { get; private set; }

    /// <summary>
    /// Validates the JWT present in <paramref name="context"/>.
    /// Returns <c>true</c> and populates <see cref="Principal"/> on success;
    /// returns <c>false</c> (and logs a warning) when the token is missing or invalid.
    /// </summary>
    protected async Task<bool> AuthenticateAsync(
        TContext          context,
        CancellationToken cancellationToken = default)
    {
        var token = context.GetToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning(
                "[{Middleware}] Message received without an authentication token — rejected.",
                GetType().Name);
            return false;
        }

        var result = await _validator.ValidateAsync(token, cancellationToken)
                                     .ConfigureAwait(false);
        if (!result.IsValid)
        {
            _logger.LogWarning(
                "[{Middleware}] Token validation failed: {Error}",
                GetType().Name, result.ErrorMessage);
            return false;
        }

        Principal = result.Principal;
        return true;
    }
}
