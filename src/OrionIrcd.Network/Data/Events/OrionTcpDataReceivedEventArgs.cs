using OrionIrcd.Network.Client;

namespace OrionIrcd.Network.Data.Events;

/// <summary>
///     Event payload containing data received from a network client.
/// </summary>
public sealed class OrionTcpDataReceivedEventArgs : EventArgs
{
    /// <summary>
    ///     Source client for the data payload.
    /// </summary>
    public OrionTcpClient Client { get; }

    /// <summary>
    ///     Received data payload.
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; }

    public OrionTcpDataReceivedEventArgs(OrionTcpClient client, ReadOnlyMemory<byte> data)
    {
        Client = client;
        Data = data;
    }
}
