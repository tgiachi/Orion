using OrionIrcd.Network.Client;

namespace OrionIrcd.Network.Data.Events;

/// <summary>
/// Event payload containing a network client instance.
/// </summary>
public sealed class OrionTcpClientEventArgs : EventArgs
{
    /// <summary>
    /// Connected or disconnected client.
    /// </summary>
    public OrionTcpClient Client { get; }

    public OrionTcpClientEventArgs(OrionTcpClient client)
    {
        Client = client;
    }
}
