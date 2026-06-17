using OrionIrcd.Network.Client;

namespace OrionIrcd.Network.Interfaces.Processing;

/// <summary>
/// Processes a framed network payload into a typed result.
/// </summary>
/// <typeparam name="T">The processed result type.</typeparam>
public interface IResultProcessor<T>
{
    /// <summary>
    /// Processes one complete framed payload for a client connection.
    /// </summary>
    /// <param name="client">The client that produced the payload.</param>
    /// <param name="data">The framed payload bytes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The processed result.</returns>
    ValueTask<T> ProcessAsync(
        OrionTcpClient client,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken
    );
}
