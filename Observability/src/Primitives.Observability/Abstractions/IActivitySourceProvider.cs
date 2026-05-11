using System.Diagnostics;

namespace Primitives.Observability.Abstractions;

/// <summary>
/// Factory for named <see cref="ActivitySource"/> instances.
/// Inject into services to create spans without coupling to a specific source name at startup.
/// </summary>
/// <remarks>
/// Sources returned by this provider must be registered with the tracer provider by name
/// (via <c>.AddActivitySource("MySource")</c> or <c>.AddSource("MySource")</c> on the
/// <c>TracerProviderBuilder</c>) for spans to be captured. Use
/// <c>ObservabilityBuilderExtensions.AddActivitySource</c> in the DI bootstrap to register them.
/// </remarks>
public interface IActivitySourceProvider
{
    /// <summary>
    /// Gets or creates a singleton <see cref="ActivitySource"/> with the specified <paramref name="name"/>.
    /// Multiple calls with the same name return the same instance.
    /// </summary>
    ActivitySource GetSource(string name);
}
