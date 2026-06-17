using Serilog.Events;

namespace OrionIrcd.Core.Logging;

/// <summary>
/// Contains data for a log event.
/// </summary>
public class LogEventData
{
    /// <summary>
    /// Gets the log level.
    /// </summary>
    public LogEventLevel Level { get; init; }

    /// <summary>
    /// Gets the timestamp of the log event.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets the formatted log message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets the exception associated with the log event, if any.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Gets the properties associated with the log event.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Properties { get; init; } = new Dictionary<string, object?>();

    /// <summary>
    /// Gets the source context (usually the logger name/class).
    /// </summary>
    public string? SourceContext { get; init; }
}
