using Serilog.Core;
using Serilog.Events;

namespace OrionIrcd.Core.Logging;

/// <summary>
/// A Serilog sink that raises events when logs are received.
/// Subscribe to <see cref="OnLogReceived" /> to receive log events.
/// </summary>
public class EventSink : ILogEventSink
{
    /// <summary>
    /// Event raised when a log event is received.
    /// </summary>
    public static event EventHandler<LogEventData>? OnLogReceived;

    /// <summary>
    /// Clears all event subscribers.
    /// Useful for cleanup or testing.
    /// </summary>
    public static void ClearSubscribers()
        => OnLogReceived = null;

    /// <summary>
    /// Emits a log event to all subscribers.
    /// </summary>
    /// <param name="logEvent">The log event to emit.</param>
    public void Emit(LogEvent logEvent)
    {
        if (OnLogReceived == null)
        {
            return;
        }

        try
        {
            // Extract properties
            var properties = new Dictionary<string, object?>();

            foreach (var property in logEvent.Properties)
            {
                properties[property.Key] = property.Value.ToString().Trim('"');
            }

            // Extract source context if available
            string? sourceContext = null;

            if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContextValue))
            {
                sourceContext = sourceContextValue.ToString().Trim('"');
            }

            // Create event data
            var eventData = new LogEventData
            {
                Level = logEvent.Level,
                Timestamp = logEvent.Timestamp,
                Message = logEvent.RenderMessage(),
                Exception = logEvent.Exception,
                Properties = properties,
                SourceContext = sourceContext
            };

            // Raise the event
            OnLogReceived?.Invoke(null, eventData);
        }
        catch
        {
            // Silently fail to avoid breaking the logging pipeline
            // Could log to Debug or another sink if needed
        }
    }
}
