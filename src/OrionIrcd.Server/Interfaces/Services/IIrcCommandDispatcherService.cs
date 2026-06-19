using OrionIrcd.IRC.Interfaces;
using OrionIrcd.Server.Core.Data.Sessions;

namespace OrionIrcd.Server.Interfaces.Services;

/// <summary>
/// Dispatches typed IRC commands to registered command listeners.
/// </summary>
public interface IIrcCommandDispatcherService
{
    /// <summary>
    /// Dispatches a command for a network session.
    /// </summary>
    /// <param name="session">The network session that produced the command.</param>
    /// <param name="command">The parsed IRC command.</param>
    /// <param name="cancellationToken">A token used to cancel the dispatch operation.</param>
    /// <returns>A task representing the dispatch operation.</returns>
    Task DispatchAsync(NetworkSession session, IIrcCommand command, CancellationToken cancellationToken = default);
}
