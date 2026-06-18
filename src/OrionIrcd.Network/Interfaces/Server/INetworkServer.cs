using OrionIrcd.Core.Types;

namespace OrionIrcd.Network.Interfaces.Server;

/// <summary>
/// Represents a network listener with common lifecycle and metadata.
/// </summary>
public interface INetworkServer : IAsyncDisposable
{
    /// <summary>
    /// Transport type exposed by this server.
    /// </summary>
    ServerType ServerType { get; }

    /// <summary>
    /// Current listening port. Returns 0 when no concrete port is bound.
    /// </summary>
    int Port { get; }

    /// <summary>
    /// Indicates whether the server is currently running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Starts the listener.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the listener has started.</returns>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stops the listener.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the listener has stopped.</returns>
    Task StopAsync(CancellationToken cancellationToken);
}
