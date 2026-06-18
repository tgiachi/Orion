using OrionIrcd.IRC.Interfaces;
using OrionIrcd.Server.Data.Listeners;

namespace OrionIrcd.Server.Interfaces.Listeners;

/// <summary>
/// Handles a typed IRC command for a network session.
/// </summary>
/// <typeparam name="TCommand">The IRC command type handled by the listener.</typeparam>
public interface IIrcCommandListener<TCommand>
    where TCommand : IIrcCommand
{
    /// <summary>
    /// Handles the command context.
    /// </summary>
    /// <param name="context">The command context with the originating session.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task representing the listener execution.</returns>
    ValueTask HandleCommandAsync(IrcCommandListenerContext<TCommand> context, CancellationToken cancellationToken = default);
}
