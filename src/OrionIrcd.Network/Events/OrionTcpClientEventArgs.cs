using OrionIrcd.Network.Client;

namespace OrionIrcd.Network.Events;

/// <summary>
///     Event payload containing a network client instance.
/// </summary>
public sealed class OrionTcpClientEventArgs : EventArgs
{
    public OrionTcpClientEventArgs(OrionTcpClient client)
    {
        Client = client;
    }

    /// <summary>
    ///     Connected or disconnected client.
    /// </summary>
    public OrionTcpClient Client { get; }
}
