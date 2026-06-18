using OrionIrcd.Network.Client;

namespace OrionIrcd.Network.Data.Events;

/// <summary>
/// Event payload containing an exception raised by WebSocket server or client network loops.
/// </summary>
public sealed class OrionWebSocketExceptionEventArgs : EventArgs
{
    /// <summary>
    /// Exception raised by the networking component.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Client related to the exception, when available.
    /// </summary>
    public OrionWebSocketClient? Client { get; }

    public OrionWebSocketExceptionEventArgs(Exception exception, OrionWebSocketClient? client = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        Exception = exception;
        Client = client;
    }
}
