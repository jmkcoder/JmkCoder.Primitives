namespace Primitives.Storage.Models;

/// <summary>HTTP verb allowed by a signed URL.</summary>
public enum SignedUrlVerb
{
    /// <summary>Allow read (download).</summary>
    Get = 0,

    /// <summary>Allow write (upload/replace).</summary>
    Put = 1,

    /// <summary>Allow deletion.</summary>
    Delete = 2,
}

/// <summary>Options for generating a pre-signed (time-limited) object URL.</summary>
public sealed class SignedUrlOptions
{
    /// <summary>How long the URL remains valid. Defaults to 1 hour.</summary>
    public TimeSpan Expiry { get; set; } = TimeSpan.FromHours(1);

    /// <summary>HTTP verb the URL permits. Defaults to <see cref="SignedUrlVerb.Get"/>.</summary>
    public SignedUrlVerb Verb { get; set; } = SignedUrlVerb.Get;
}
