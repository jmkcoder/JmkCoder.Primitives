using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Primitives.Observability.Extensions;
using Serilog;

namespace Primitives.Observability.Serilog.Extensions;

public static class SerilogObservabilityExtensions
{
    /// <summary>
    /// Replaces the default <see cref="Microsoft.Extensions.Logging"/> providers with Serilog.
    /// </summary>
    /// <param name="builder">The observability builder.</param>
    /// <param name="configure">
    /// Delegate to configure the <see cref="LoggerConfiguration"/> — add sinks, enrichers,
    /// minimum levels, and filters. For OTLP log export add
    /// <c>WriteTo.OpenTelemetry(…)</c> from the <c>Serilog.Sinks.OpenTelemetry</c> package.
    /// </param>
    /// <param name="clearExistingProviders">
    /// When <c>true</c> (default), existing <see cref="ILoggerProvider"/>s are cleared so that
    /// Serilog is the sole logging backend, preventing duplicate log entries.
    /// </param>
    /// <returns>The same <see cref="ObservabilityBuilder"/> for further chaining.</returns>
    public static ObservabilityBuilder WithSerilog(
        this ObservabilityBuilder builder,
        Action<LoggerConfiguration>? configure = null,
        bool clearExistingProviders = true)
    {
        var loggerConfig = new LoggerConfiguration()
            .Enrich.FromLogContext();

        configure?.Invoke(loggerConfig);

        var serilogLogger = loggerConfig.CreateLogger();
        Log.Logger = serilogLogger;

        builder.Services.AddLogging(logging =>
        {
            if (clearExistingProviders)
                logging.ClearProviders();

            logging.AddSerilog(serilogLogger, dispose: true);
        });

        return builder;
    }
}
