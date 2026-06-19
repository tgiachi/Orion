using OrionIrcd.Server.Data.Sessions;

namespace OrionIrcd.Server.Interfaces.Services;

/// <summary>
/// Loads and sends IRC MOTD content.
/// </summary>
public interface IIrcMotdService
{
    /// <summary>
    /// Gets the configured MOTD lines.
    /// </summary>
    /// <returns>Configured MOTD lines, or an empty list when MOTD is unavailable.</returns>
    IReadOnlyList<string> GetMotdLines();

    /// <summary>
    /// Sends MOTD numerics to a session.
    /// </summary>
    /// <param name="session">Target network session.</param>
    /// <param name="target">IRC nickname or star target.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendMotdAsync(NetworkSession session, string target, CancellationToken cancellationToken);
}
