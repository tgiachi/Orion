using OrionIrcd.Network.Client;

namespace OrionIrcd.Network.Events;

/// <summary>
///     Event payload containing an exception raised by server or client network loops.
/// </summary>
public sealed class OrionTcpExceptionEventArgs : EventArgs
{
    public OrionTcpExceptionEventArgs(Exception exception, OrionTcpClient? client = null)
    {
        Exception = exception;
        Client = client;
    }

    /// <summary>
    ///     Exception raised by the networking component.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    ///     Client related to the exception, when available.
    /// </summary>
    public OrionTcpClient? Client { get; }
}
