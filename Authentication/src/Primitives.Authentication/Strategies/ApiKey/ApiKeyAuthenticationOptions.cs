using System.ComponentModel.DataAnnotations;

namespace Primitives.Authentication.Strategies.ApiKey;

/// <summary>
/// Configuration for API Key authentication.
/// The strategy returns the API key formatted as the requested Authorization header value,
/// or supplies it as a custom header / query parameter depending on <see cref="Placement"/>.
/// </summary>
public sealed class ApiKeyAuthenticationOptions
{
    /// <summary>The secret API key value.</summary>
    [Required]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Where the API key should be sent. Defaults to <see cref="ApiKeyPlacement.Header"/>.</summary>
    public ApiKeyPlacement Placement { get; set; } = ApiKeyPlacement.Header;

    /// <summary>
    /// Header or query-parameter name that carries the key.
    /// Defaults to "X-API-Key".
    /// When <see cref="Placement"/> is <see cref="ApiKeyPlacement.BearerToken"/> this is ignored.
    /// </summary>
    public string KeyName { get; set; } = "X-API-Key";

    /// <summary>
    /// Prefix prepended to the key value in the header, e.g. "ApiKey " → "ApiKey &lt;key&gt;".
    /// Only used when <see cref="Placement"/> is <see cref="ApiKeyPlacement.Header"/>.
    /// Leave empty for no prefix.
    /// </summary>
    public string HeaderPrefix { get; set; } = string.Empty;
}

/// <summary>Determines how the API key is delivered to the server.</summary>
public enum ApiKeyPlacement
{
    /// <summary>Sent in a custom request header (default: X-API-Key).</summary>
    Header,

    /// <summary>Sent as a URL query parameter.</summary>
    QueryParameter,

    /// <summary>Sent in the Authorization header as "Bearer {key}".</summary>
    BearerToken
}
