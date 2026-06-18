using OrionIrcd.Server.Data.IRC;

namespace OrionIrcd.Server.Interfaces.Services;

/// <summary>
/// Tracks IRC registration state for network sessions.
/// </summary>
public interface IIrcSessionStateService
{
    /// <summary>
    /// Gets a state snapshot, creating empty state when missing.
    /// </summary>
    /// <param name="sessionId">Network session id.</param>
    /// <returns>Current IRC state snapshot.</returns>
    IrcSessionStateSnapshot GetSnapshot(long sessionId);

    /// <summary>
    /// Removes IRC state for a disconnected session.
    /// </summary>
    /// <param name="sessionId">Network session id.</param>
    void Remove(long sessionId);

    /// <summary>
    /// Stores USER registration values.
    /// </summary>
    /// <param name="sessionId">Network session id.</param>
    /// <param name="username">IRC username.</param>
    /// <param name="realName">IRC real name.</param>
    void SetUser(long sessionId, string username, string realName);

    /// <summary>
    /// Marks the configured server PASS as accepted for a session.
    /// </summary>
    /// <param name="sessionId">Network session id.</param>
    void SetPassAccepted(long sessionId);

    /// <summary>
    /// Attempts to complete registration when NICK and USER are present.
    /// </summary>
    /// <param name="sessionId">Network session id.</param>
    /// <param name="snapshot">Registered snapshot when registration transitioned to complete.</param>
    /// <returns>True only when this call completes registration.</returns>
    bool TryMarkRegistered(long sessionId, out IrcSessionStateSnapshot? snapshot);

    /// <summary>
    /// Attempts to complete registration when NICK, USER, and any required PASS are present.
    /// </summary>
    /// <param name="sessionId">Network session id.</param>
    /// <param name="isPassRequired">Whether PASS must be accepted before registration.</param>
    /// <param name="snapshot">Registered snapshot when registration transitioned to complete.</param>
    /// <returns>True only when this call completes registration.</returns>
    bool TryMarkRegistered(long sessionId, bool isPassRequired, out IrcSessionStateSnapshot? snapshot);

    /// <summary>
    /// Attempts to get existing IRC state without creating it.
    /// </summary>
    /// <param name="sessionId">Network session id.</param>
    /// <param name="snapshot">Current state when found.</param>
    /// <returns>True when state exists.</returns>
    bool TryGetSnapshot(long sessionId, out IrcSessionStateSnapshot? snapshot);

    /// <summary>
    /// Attempts to set a nickname for a session.
    /// </summary>
    /// <param name="sessionId">Network session id.</param>
    /// <param name="nickname">Requested nickname.</param>
    /// <returns>False when nickname is empty or used by another session.</returns>
    bool TrySetNickname(long sessionId, string nickname);
}
