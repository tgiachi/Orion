using OrionIrcd.Network.Client;

namespace OrionIrcd.Network.Interfaces.Middleware;

/// <summary>
/// Transforms raw network bytes for a client connection.
/// </summary>
/// <remarks>
/// Middleware operates on raw bytes and MUST NOT assume any message, packet, or frame
/// semantics — framing and protocol parsing are the consumer's responsibility, applied
/// to the <c>OnDataReceived</c> output of the client. Returning
/// <see cref="ReadOnlyMemory{T}.Empty" /> from either method drops the payload and
/// short-circuits the remaining pipeline.
/// </remarks>
public interface INetMiddleware
{
    /// <summary>
    /// Transforms an incoming payload before it is dispatched to consumers.
    /// </summary>
    /// <param name="client">Client associated with the payload, if available.</param>
    /// <param name="data">Incoming bytes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transformed bytes, or <see cref="ReadOnlyMemory{T}.Empty" /> to drop the payload.</returns>
    ValueTask<ReadOnlyMemory<byte>> ProcessAsync(
        OrionTcpClient? client,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Transforms an outgoing payload before it is written to the socket.
    /// </summary>
    /// <param name="client">Client associated with the payload, if available.</param>
    /// <param name="data">Outgoing bytes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transformed bytes, or <see cref="ReadOnlyMemory{T}.Empty" /> to drop the payload.</returns>
    ValueTask<ReadOnlyMemory<byte>> ProcessSendAsync(
        OrionTcpClient? client,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default
    )
        => ValueTask.FromResult(data);
}
