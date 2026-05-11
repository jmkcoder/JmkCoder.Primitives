using System.Diagnostics;

namespace Primitives.Authentication.Diagnostics;

/// <summary>
/// Central <see cref="ActivitySource"/> for the Primitives.Authentication library.
/// Listen to source name <c>"Primitives.Authentication"</c> in your OpenTelemetry pipeline:
/// <code>
/// builder.WithTracing(t => t.AddSource("Primitives.Authentication"));
/// </code>
/// </summary>
public static class AuthenticationDiagnostics
{
    /// <summary>The OpenTelemetry activity source name.</summary>
    public const string SourceName = "Primitives.Authentication";

    /// <summary>Shared <see cref="ActivitySource"/> instance for the library.</summary>
    public static readonly ActivitySource Source = new(SourceName, "1.0.0");

    // ── Well-known activity names ────────────────────────────────────────────

    internal const string ActivityAuthenticate     = "authentication.authenticate";
    internal const string ActivityRefresh          = "authentication.refresh";
    internal const string ActivityValidateToken    = "authentication.validate_token";
    internal const string ActivityStrategyExecute  = "authentication.strategy.execute";

    // ── Well-known tag keys ──────────────────────────────────────────────────

    internal const string TagStrategyName   = "auth.strategy.name";
    internal const string TagIsSuccess      = "auth.success";
    internal const string TagErrorMessage   = "auth.error";
    internal const string TagSubject        = "auth.subject";
}
