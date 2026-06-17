using OrionIrcd.Network.Client;

namespace OrionIrcd.Network.Events;

/// <summary>
///     Event payload containing a WebSocket network client instance.
/// </summary>
public sealed class OrionWebSocketClientEventArgs : EventArgs
{
    /// <summary>
    ///     Connected or disconnected client.
    /// </summary>
    public OrionWebSocketClient Client { get; }

    public OrionWebSocketClientEventArgs(OrionWebSocketClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        Client = client;
    }
}
