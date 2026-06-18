using OrionIrcd.Network.Client;

namespace OrionIrcd.Network.Data.Events;

/// <summary>
/// Event payload containing data received from a WebSocket network client.
/// </summary>
public sealed class OrionWebSocketDataReceivedEventArgs : EventArgs
{
    /// <summary>
    /// Source client for the data payload.
    /// </summary>
    public OrionWebSocketClient Client { get; }

    /// <summary>
    /// Received data payload.
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; }

    public OrionWebSocketDataReceivedEventArgs(OrionWebSocketClient client, ReadOnlyMemory<byte> data)
    {
        ArgumentNullException.ThrowIfNull(client);

        Client = client;
        Data = data;
    }
}
