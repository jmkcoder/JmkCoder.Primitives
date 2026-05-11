namespace Primitives.Authentication.Exceptions;

/// <summary>Thrown when an authentication strategy encounters a non-recoverable error.</summary>
public sealed class AuthenticationException : Exception
{
    /// <summary>Name of the strategy that failed (or was requested but not found).</summary>
    public string StrategyName { get; }

    /// <summary>Structured reason code for the failure.</summary>
    public AuthenticationFailureReason Reason { get; }

    public AuthenticationException(
        string strategyName,
        string message,
        AuthenticationFailureReason reason = AuthenticationFailureReason.Unknown)
        : base(message)
    {
        StrategyName = strategyName;
        Reason       = reason;
    }

    public AuthenticationException(
        string strategyName,
        string message,
        Exception innerException,
        AuthenticationFailureReason reason = AuthenticationFailureReason.Unknown)
        : base(message, innerException)
    {
        StrategyName = strategyName;
        Reason       = reason;
    }
}

/// <summary>Structured reason codes for <see cref="AuthenticationException"/>.</summary>
public enum AuthenticationFailureReason
{
    Unknown = 0,

    /// <summary>The requested strategy name is not registered.</summary>
    StrategyNotFound,

    /// <summary>Required options are missing or invalid.</summary>
    InvalidConfiguration,

    /// <summary>The identity provider rejected the credentials.</summary>
    CredentialRejected,

    /// <summary>A network or transport error occurred.</summary>
    TransportError,

    /// <summary>The platform does not support the requested mechanism (e.g. Kerberos on Linux without GSSAPI).</summary>
    PlatformNotSupported,
}

