using OrionIrcd.Core.Interfaces.Services;
using OrionIrcd.Network.Interfaces.Client;
using OrionIrcd.Server.Data.Sessions;

namespace OrionIrcd.Server.Interfaces.Services;

/// <summary>
/// Manages active network sessions and exposes direct session commands.
/// </summary>
public interface ISessionManagerService : IOrionIrcdService
{
    /// <summary>
    /// Closes an active session.
    /// </summary>
    /// <param name="sessionId">Target session id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true" /> when the session was found and close was requested.</returns>
    Task<bool> CloseAsync(long sessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a snapshot of active sessions.
    /// </summary>
    /// <returns>Active session snapshot.</returns>
    IReadOnlyList<NetworkSession> GetSessions();

    /// <summary>
    /// Records raw data activity for a connection.
    /// </summary>
    /// <param name="connection">Source network transport.</param>
    /// <param name="data">Received bytes.</param>
    /// <returns>The updated session.</returns>
    NetworkSession RecordActivity(INetworkConnection connection, ReadOnlyMemory<byte> data);

    /// <summary>
    /// Registers a connected network connection as an active session.
    /// </summary>
    /// <param name="connection">Connected network transport.</param>
    /// <returns>The active session for the connection.</returns>
    NetworkSession Register(INetworkConnection connection);

    /// <summary>
    /// Sends bytes to an active session.
    /// </summary>
    /// <param name="sessionId">Target session id.</param>
    /// <param name="payload">Bytes to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true" /> when the payload was sent.</returns>
    Task<bool> SendAsync(long sessionId, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to get an active session by transport session id.
    /// </summary>
    /// <param name="sessionId">Transport session id.</param>
    /// <param name="session">Matched session when found.</param>
    /// <returns><see langword="true" /> when the session exists.</returns>
    bool TryGetSession(long sessionId, out NetworkSession? session);

    /// <summary>
    /// Removes a connection from the active session list.
    /// </summary>
    /// <param name="connection">Disconnected network transport.</param>
    /// <returns>The removed session, or null when the connection was not tracked.</returns>
    NetworkSession? Unregister(INetworkConnection connection);
}
