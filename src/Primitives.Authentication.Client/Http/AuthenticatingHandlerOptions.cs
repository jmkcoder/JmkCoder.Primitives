namespace Primitives.Authentication.Client.Http;

/// <summary>
/// Options for <see cref="AuthenticatingHandler"/>, bound by the named-options
/// name used when registering the handler (defaults to the HttpClient name).
/// </summary>
public sealed class AuthenticatingHandlerOptions
{
    /// <summary>
    /// Name of the registered <c>IAuthenticationStrategy</c> to use for token acquisition
    /// (e.g. "OIDC", "UsernamePassword", "ApiKey").
    /// </summary>
    public string StrategyName { get; set; } = string.Empty;

    /// <summary>HTTP header to write the token into. Default: <c>Authorization</c>.</summary>
    public string HeaderName { get; set; } = "Authorization";

    /// <summary>Token type prefix. Default: <c>Bearer</c>.</summary>
    public string TokenPrefix { get; set; } = "Bearer";
}
