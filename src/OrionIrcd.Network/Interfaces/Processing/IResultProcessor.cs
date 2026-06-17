using OrionIrcd.Network.Interfaces.Client;

namespace OrionIrcd.Network.Interfaces.Processing;

/// <summary>
/// Processes a framed network payload into a typed result.
/// </summary>
/// <typeparam name="T">The processed result type.</typeparam>
public interface IResultProcessor<T>
{
    /// <summary>
    /// Processes one complete framed payload for a network connection.
    /// </summary>
    /// <param name="connection">The connection that produced the payload.</param>
    /// <param name="data">The framed payload bytes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The processed result.</returns>
    ValueTask<T> ProcessAsync(
        INetworkConnection connection,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken
    );
}
