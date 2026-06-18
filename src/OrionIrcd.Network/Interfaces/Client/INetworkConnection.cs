using System.Net;

namespace OrionIrcd.Network.Interfaces.Client;

/// <summary>
/// Represents a connected network client independently from the underlying transport.
/// </summary>
public interface INetworkConnection
{
    /// <summary>
    /// Unique connection identifier assigned by the transport.
    /// </summary>
    long SessionId { get; }

    /// <summary>
    /// Remote endpoint when the transport exposes one.
    /// </summary>
    EndPoint? RemoteEndPoint { get; }

    /// <summary>
    /// Indicates whether the connection is still open.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Closes the connection.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the connection has closed.</returns>
    Task CloseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends raw bytes to the connected client.
    /// </summary>
    /// <param name="payload">The payload bytes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the payload has been written.</returns>
    Task SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken);
}
