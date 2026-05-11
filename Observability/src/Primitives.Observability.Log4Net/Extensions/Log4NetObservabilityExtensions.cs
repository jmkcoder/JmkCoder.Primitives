using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Primitives.Observability.Extensions;

namespace Primitives.Observability.Log4Net.Extensions;

public static class Log4NetObservabilityExtensions
{
    /// <summary>
    /// Adds log4net as a <see cref="Microsoft.Extensions.Logging"/> provider, routing log entries
    /// to log4net appenders configured in <paramref name="configFile"/>.
    /// </summary>
    /// <param name="builder">The observability builder.</param>
    /// <param name="configFile">
    /// Path to the log4net XML configuration file.
    /// Defaults to <c>log4net.config</c> in the application's base directory.
    /// If the file does not exist log4net initialises with no appenders (silent).
    /// </param>
    /// <returns>The same <see cref="ObservabilityBuilder"/> for further chaining.</returns>
    public static ObservabilityBuilder WithLog4Net(
        this ObservabilityBuilder builder,
        string configFile = "log4net.config")
    {
        builder.Services.AddLogging(logging => logging.AddLog4Net(configFile));
        return builder;
    }
}
