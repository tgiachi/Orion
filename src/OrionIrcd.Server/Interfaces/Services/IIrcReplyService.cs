using OrionIrcd.Server.Data.Sessions;

namespace OrionIrcd.Server.Interfaces.Services;

/// <summary>
/// Sends IRC protocol lines to network sessions.
/// </summary>
public interface IIrcReplyService
{
    /// <summary>
    /// Gets the server name used in IRC replies.
    /// </summary>
    string ServerName { get; }

    /// <summary>
    /// Sends a raw IRC line and appends CRLF.
    /// </summary>
    /// <param name="session">Target session.</param>
    /// <param name="line">IRC line without CRLF.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True when the line was sent.</returns>
    Task<bool> SendLineAsync(NetworkSession session, string line, CancellationToken cancellationToken);

    /// <summary>
    /// Sends a server-prefixed numeric reply.
    /// </summary>
    /// <param name="session">Target session.</param>
    /// <param name="code">Three-digit numeric code.</param>
    /// <param name="target">Nickname or star target.</param>
    /// <param name="message">Trailing reply message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True when the line was sent.</returns>
    Task<bool> SendNumericAsync(
        NetworkSession session,
        string code,
        string target,
        string message,
        CancellationToken cancellationToken
    );
}
