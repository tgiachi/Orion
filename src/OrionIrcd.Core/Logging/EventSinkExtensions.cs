using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace OrionIrcd.Core.Logging;

/// <summary>
/// Extension methods for configuring the EventSink.
/// </summary>
public static class EventSinkExtensions
{
    /// <summary>
    /// Adds the EventSink to the Serilog pipeline.
    /// Subscribe to <see cref="EventSink.OnLogReceived" /> to receive log events.
    /// </summary>
    /// <param name="sinkConfiguration">The logger sink configuration.</param>
    /// <param name="restrictedToMinimumLevel">The minimum log level to capture. Defaults to Verbose (all logs).</param>
    /// <returns>The logger configuration for chaining.</returns>
    public static LoggerConfiguration EventSink(
        this LoggerSinkConfiguration sinkConfiguration,
        LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose
    )
        => sinkConfiguration.Sink(new EventSink(), restrictedToMinimumLevel);
}
